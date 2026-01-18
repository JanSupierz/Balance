document.addEventListener('DOMContentLoaded', function () {
    document.body.addEventListener('click', function (e) {
        const btn = e.target.closest('.js-delete-trigger');
        if (!btn) return;

        e.preventDefault();

        const modalEl = document.getElementById('deleteUserModal');
        if (!modalEl) return;

        const idInput = modalEl.querySelector('.js-delete-id');
        const titleSpan = modalEl.querySelector('.js-delete-title');

        if (idInput) idInput.value = btn.dataset.id;
        if (titleSpan) titleSpan.textContent = btn.dataset.title;

        const modalInstance = bootstrap.Modal.getOrCreateInstance(modalEl);
        modalInstance.show();
    });
});
