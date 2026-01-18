document.addEventListener('DOMContentLoaded', () => {

    const configEl = document.getElementById('task-page-config');
    if (!configEl) return;

    const token = configEl.dataset.antiforgery;
    const createTagUrl = configEl.dataset.createTagUrl;
    const deleteTagUrl = configEl.dataset.deleteTagUrl;

    const select = document.getElementById('frequencySelect');
    const container = document.getElementById('deadlineInputContainer');

    function toggleDate() {
        const selectedText = select.options[select.selectedIndex].text;
        container.classList.toggle('d-none', selectedText !== 'OneTime');
    }

    select.addEventListener('change', toggleDate);
    toggleDate();

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

    const dateInput = document.getElementById('deadlineDate');
    if (dateInput) {
        dateInput.style.userSelect = 'none';
        dateInput.addEventListener('mousedown', e => e.preventDefault());
        dateInput.addEventListener('click', () => {
            if (typeof dateInput.showPicker === 'function') {
                dateInput.showPicker();
            }
        });

        const iconSpan = document.querySelector('#deadlineInputContainer .input-group-text');
        if (iconSpan) iconSpan.style.pointerEvents = 'none';
    }

    document.getElementById('btnCreateTag')?.addEventListener('click', async () => {
        const inputName = document.getElementById('newTagName');
        const inputColor = document.getElementById('newTagColor');
        if (!inputName.value) return;

        const response = await fetch(createTagUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ name: inputName.value, color: inputColor.value })
        });

        if (!response.ok) return;

        const tag = await response.json();

        document.getElementById('noTagsMsg')?.classList.add('d-none');

        const list = document.getElementById('tagsList');
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
            <label class="badge rounded-pill fw-normal tag-label badge-tag d-flex align-items-center gap-2"
                   for="tag_${tag.id}">
                ${tag.name}
            </label>
            <button type="button" class="btn-delete-tag" data-id="${tag.id}">
                <i class="bi bi-x"></i>
            </button>
        `;

        list.appendChild(wrapper);

        const cb = wrapper.querySelector('.tag-checkbox');
        updateTagVisuals(cb);
        cb.addEventListener('change', () => updateTagVisuals(cb));

        inputName.value = '';
    });

    document.body.addEventListener('click', async e => {
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

        if (document.getElementById('tagsList')?.children.length === 0) {
            document.getElementById('noTagsMsg')?.classList.remove('d-none');
        }
    });
});
