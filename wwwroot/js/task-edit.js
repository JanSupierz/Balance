document.addEventListener('DOMContentLoaded', () => {

    const config = document.getElementById('task-edit-config');
    if (!config) return;

    const token = config.dataset.antiforgery;
    const createTagUrl = config.dataset.createTagUrl;
    const deleteTagUrl = config.dataset.deleteTagUrl;

    /* =============================
       Frequency / Deadline toggle
       ============================= */

    const select = document.getElementById('frequencySelect');
    const container = document.getElementById('deadlineInputContainer');

    function toggleDate() {
        const text = select.options[select.selectedIndex].text;
        container.classList.toggle('d-none', text !== 'OneTime');
    }

    select.addEventListener('change', toggleDate);
    toggleDate();

    /* =============================
       Date picker behavior
       ============================= */

    const dateInput = document.getElementById('deadlineDate');
    if (dateInput) {
        dateInput.style.userSelect = 'none';
        dateInput.addEventListener('mousedown', e => e.preventDefault());

        dateInput.addEventListener('click', () => {
            if (typeof dateInput.showPicker === 'function') {
                dateInput.showPicker();
            }
        });
    }

    /* =============================
       Tag visuals
       ============================= */

    function updateTagVisuals(checkbox) {
        const label = document.querySelector(`label[for="${checkbox.id}"]`);
        if (!label) return;

        const color = checkbox.dataset.color || '#4f46e5';

        if (checkbox.checked) {
            label.style.backgroundColor = color;
            label.style.color = '#ffffff';
            label.style.borderColor = color;
        } else {
            label.style.backgroundColor = 'var(--bg-surface)';
            label.style.color = 'var(--text-main)';
            label.style.borderColor = 'var(--border-color)';
        }
    }

    document.querySelectorAll('.tag-checkbox').forEach(cb => {
        updateTagVisuals(cb);
        cb.addEventListener('change', () => updateTagVisuals(cb));
    });

    /* =============================
       Create tag
       ============================= */

    document.getElementById('btnCreateTag')
        ?.addEventListener('click', async () => {

            const nameInput = document.getElementById('newTagName');
            const colorInput = document.getElementById('newTagColor');

            if (!nameInput.value) return;

            const response = await fetch(createTagUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({
                    name: nameInput.value,
                    color: colorInput.value
                })
            });

            if (!response.ok) return;

            const tag = await response.json();
            const list = document.getElementById('tagsList');

            document.getElementById('noTagsMsg')?.classList.add('d-none');

            const wrapper = document.createElement('div');
            wrapper.className = 'tag-wrapper';
            wrapper.id = `tag-wrapper-${tag.id}`;

            wrapper.innerHTML = `
                <input class="form-check-input d-none tag-checkbox"
                       type="checkbox"
                       name="selectedTagIds"
                       value="${tag.id}"
                       id="tag_${tag.id}"
                       data-color="${tag.color}"
                       checked>

                <label class="badge rounded-pill fw-normal tag-label badge-tag"
                       for="tag_${tag.id}">
                    ${tag.name}
                </label>

                <button type="button"
                        class="btn-delete-tag"
                        data-id="${tag.id}">
                    <i class="bi bi-x"></i>
                </button>
            `;

            list.appendChild(wrapper);

            const cb = wrapper.querySelector('.tag-checkbox');
            updateTagVisuals(cb);
            cb.addEventListener('change', () => updateTagVisuals(cb));

            nameInput.value = '';
        });

    /* =============================
       Delete tag (delegation)
       ============================= */

    document.getElementById('tagsList')
        ?.addEventListener('click', async e => {

            const btn = e.target.closest('.btn-delete-tag');
            if (!btn) return;

            const id = btn.dataset.id;
            if (!confirm('Delete this tag?')) return;

            const response = await fetch(deleteTagUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({ id })
            });

            if (!response.ok) return;

            document.getElementById(`tag-wrapper-${id}`)?.remove();

            if (!document.getElementById('tagsList').children.length) {
                document.getElementById('noTagsMsg')?.classList.remove('d-none');
            }
        });
});
