document.querySelectorAll('.share-link').forEach(button => {
    button.addEventListener('click', async () => {
        const url = button.dataset.url;
        const feedback = document.getElementById('share-feedback');
        const manual = document.getElementById('share-manual');
        manual.hidden = true;
        try {
            await navigator.clipboard.writeText(url);
            feedback.textContent = 'Đã sao chép liên kết xác minh.';
        } catch {
            manual.value = url;
            manual.hidden = false;
            manual.focus();
            manual.select();
            feedback.textContent = 'Liên kết đã được chọn. Nhấn Ctrl+C hoặc chọn Sao chép trên điện thoại.';
        }
    });
});
