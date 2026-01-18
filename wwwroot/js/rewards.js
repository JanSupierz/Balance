document.addEventListener('DOMContentLoaded', function () {

    // DELETE modal
    const deleteModal = document.getElementById('deletePrizeModal');
    if (deleteModal) {
        deleteModal.addEventListener('show.bs.modal', event => {
            const button = event.relatedTarget;
            const id = button.getAttribute('data-id');
            const title = button.getAttribute('data-title');
            deleteModal.querySelector('.js-delete-title').textContent = title;
            deleteModal.querySelector('.js-delete-id').value = id;
        });
    }

    // EDIT modal
    const editModal = document.getElementById('editPrizeModal');
    if (editModal) {
        editModal.addEventListener('show.bs.modal', event => {
            const button = event.relatedTarget;
            const id = button.getAttribute('data-id');
            const title = button.getAttribute('data-title');
            const cost = button.getAttribute('data-cost');
            const description = button.getAttribute('data-description');

            editModal.querySelector('#editPrizeId').value = id;
            editModal.querySelector('#editPrizeTitle').value = title;
            editModal.querySelector('#editPrizeCost').value = cost;
            editModal.querySelector('#editPrizeDesc').value = description;
        });
    }

    // Card animation
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const progressBar = entry.target.querySelector('.progress-bar');
                if (progressBar) {
                    const width = progressBar.style.width;
                    progressBar.style.width = '0';
                    void progressBar.offsetWidth;
                    progressBar.style.width = width;
                }
            }
        });
    }, { threshold: 0.5 });

    document.querySelectorAll('.card-reward').forEach(card => observer.observe(card));
});