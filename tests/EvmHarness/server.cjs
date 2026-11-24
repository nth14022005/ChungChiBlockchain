// Isolated disposable EVM. No public network or real funds; only listens on loopback.
const ganache = require('ganache');
const solc = require('solc');
const { JsonRpcProvider, ContractFactory } = require('ethers');
const fs = require('fs'), path = require('path');
(async()=>{
 const root=path.resolve(__dirname,'../..');
 const input={language:'Solidity',sources:{'CertificateRegistry.sol':{content:fs.readFileSync(path.join(root,'contracts/CertificateRegistry.sol'),'utf8')}},settings:{outputSelection:{'*':{'*':['abi','evm.bytecode.object']}}}};
 const output=JSON.parse(solc.compile(JSON.stringify(input)));
 const errors=(output.errors||[]).filter(e=>e.severity==='error');if(errors.length)throw new Error(errors.map(e=>e.formattedMessage).join('\n'));
 const artifact=output.contracts['CertificateRegistry.sol'].CertificateRegistry;
 const server=ganache.server({chain:{chainId:1337},wallet:{totalAccounts:3},miner:{blockTime:1},logging:{quiet:true}});
 await server.listen(18545,'127.0.0.1');
 const provider=new JsonRpcProvider('http://127.0.0.1:18545');const owner=await provider.getSigner(0);const second=await provider.getSigner(1);
 const contract=await new ContractFactory(artifact.abi,artifact.evm.bytecode.object,owner).deploy();await contract.waitForDeployment();
 const other=await new ContractFactory(artifact.abi,artifact.evm.bytecode.object,owner).deploy();await other.waitForDeployment();
 fs.writeFileSync(path.join(__dirname,'fixture.local.json'),JSON.stringify({rpcUrl:'http://127.0.0.1:18545',chainId:1337,contractAddress:await contract.getAddress(),otherContract:await other.getAddress(),owner:await owner.getAddress(),otherWallet:await second.getAddress()}));
 console.log('LOCAL EVM READY; fixture.local.json contains public addresses only. Stop with Ctrl+C.');
})().catch(e=>{console.error(e.message);process.exit(1)});
