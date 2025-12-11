// Global Balance App Namespace
window.Balance = {

    // --- 1. Generic Delete Modal Handler ---
    // Usage: <button class="js-delete-trigger" data-id="5" data-title="My Item" data-target="#myModal">
    // Requires: Modal inputs to have classes 'js-delete-id' and 'js-delete-title'
    initDeleteHandlers: function () {
        document.body.addEventListener('click', function (e) {
            const btn = e.target.closest('.js-delete-trigger');
            if (btn) {
                e.preventDefault();
                const id = btn.dataset.id;
                const title = btn.dataset.title;
                const targetModalId = btn.dataset.target; // e.g., "#deleteTaskModal"

                const modalEl = document.querySelector(targetModalId);
                if (modalEl) {
                    // Find inputs inside the specific modal
                    const idInput = modalEl.querySelector('.js-delete-id');
                    const titleSpan = modalEl.querySelector('.js-delete-title');

                    if (idInput) idInput.value = id;
                    if (titleSpan) titleSpan.textContent = title;

                    const modalInstance = new bootstrap.Modal(modalEl);
                    modalInstance.show();
                }
            }
        });
    },

    // --- 2. Tag Visuals Logic ---
    updateTagVisuals: function (checkbox) {
        // Finds label associated with checkbox ID
        const label = document.querySelector(`label[for="${checkbox.id}"]`);
        if (!label) return;

        const color = checkbox.dataset.color || '#4f46e5';

        if (checkbox.checked) {
            label.style.backgroundColor = color;
            label.style.color = '#ffffff';
            label.style.borderColor = color;
        } else {
            label.style.backgroundColor = '#ffffff';
            label.style.color = color;
            label.style.borderColor = color;
        }
    },

    initTags: function () {
        const checkboxes = document.querySelectorAll('.tag-checkbox');
        checkboxes.forEach(cb => {
            Balance.updateTagVisuals(cb); // Init state
            cb.addEventListener('change', () => Balance.updateTagVisuals(cb));
        });
    },

    // --- 3. Toast Helper ---
    showToast: function (elementId) {
        const el = document.getElementById(elementId);
        if (el) {
            const bsToast = new bootstrap.Toast(el);
            bsToast.show();
        }
    }
};

// Initialize Global Handlers on Load
document.addEventListener('DOMContentLoaded', function () {
    Balance.initDeleteHandlers();
    Balance.initTags();
});

// --- Theme Switcher Logic ---

function setTheme(themeName) {
    // 1. Set the attribute on the HTML tag
    if (themeName === 'default') {
        document.documentElement.removeAttribute('data-theme');
    } else {
        document.documentElement.setAttribute('data-theme', themeName);
    }

    // 2. Save to LocalStorage
    localStorage.setItem('balance-theme', themeName);
}

// 3. Initialize Theme on Load (Run this immediately)
(function () {
    const savedTheme = localStorage.getItem('balance-theme');
    if (savedTheme && savedTheme !== 'default') {
        document.documentElement.setAttribute('data-theme', savedTheme);
    }
})();