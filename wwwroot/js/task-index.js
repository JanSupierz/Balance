document.addEventListener('DOMContentLoaded', function () {
    const config = document.getElementById('task-index-config');
    if (!config) return;

    const token = config.dataset.antiforgery;
    const toggleUrl = config.dataset.toggleUrl;
    const revertUrl = config.dataset.revertUrl;

    const activeList = document.getElementById('active-list');
    const completedList = document.getElementById('completed-list');
    const activeCountEl = document.getElementById('active-count');
    const completedCountEl = document.getElementById('completed-count');
    const completedNavItem = document.getElementById('completed-nav-item');
    const activeEmptyState = document.getElementById('active-empty-state');
    const undoToast = new bootstrap.Toast(document.getElementById('undoToast'), { delay: 4000 });
    const undoBtn = document.getElementById('btn-undo-action');
    let lastModifiedTaskId = null;

    // Tabs Styling Logic
    const tabs = document.querySelectorAll('.nav-link');
    tabs.forEach(tab => {
        tab.addEventListener('show.bs.tab', function () {
            tabs.forEach(t => {
                t.classList.remove('active', 'shadow-sm');
                t.classList.add('text-muted');
                t.style.backgroundColor = '';
                t.style.color = '';
            });
            this.classList.add('active', 'shadow-sm');
            this.classList.remove('text-muted');
            this.style.backgroundColor = 'var(--bg-surface)';
            this.style.color = 'var(--text-main)';
        });
    });

    activeList.addEventListener('click', function (e) {
        const btn = e.target.closest('.btn-complete');
        if (btn) toggleTask(btn.closest('.task-col').dataset.taskId, btn.closest('.task-col'), btn);
    });

    undoBtn.addEventListener('click', function () {
        if (lastModifiedTaskId) { revertTask(lastModifiedTaskId); undoToast.hide(); }
    });

    document.body.addEventListener('click', function (e) {
        const btn = e.target.closest('.js-delete-trigger');
        if (btn && btn.dataset.target === "#deleteTaskModal") {
            e.preventDefault();
            const modalEl = document.getElementById('deleteTaskModal');
            if (modalEl) {
                modalEl.querySelector('.js-delete-id').value = btn.dataset.id;
                modalEl.querySelector('.js-delete-title').textContent = btn.dataset.title;
                const modalInstance = bootstrap.Modal.getOrCreateInstance(modalEl);
                modalInstance.show();
            }
        }
    });

    // Helper to generate the specific "Check In" button HTML
    function getCheckInButtonHtml(btnClass, points) {
        return `
                    <button type="button" class="btn btn-sm btn-complete ${btnClass} w-100 shadow-sm d-flex align-items-center justify-content-between ps-3 pe-2 py-2" style="transition: all 0.3s ease;">
                        <span class="fw-bold"><i class="bi bi-check-lg me-1"></i>Check In</span>
                        <span class="badge rounded-pill d-flex align-items-center"
                              style="background-color: var(--bg-surface); color: var(--text-main); border: 1px solid rgba(0,0,0,0.1);"
                              title="You earn ${points} points for this action">
                            +${points} <i class="bi bi-star-fill text-warning ms-1"></i>
                        </span>
                    </button>
                `;
    }

    async function toggleTask(taskId, cardCol, btn) {
        cardCol.dataset.isMoving = "to-completed";
        btn.disabled = true; const originalText = btn.innerHTML;
        try {
            const response = await fetch(toggleUrl, { method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token }, body: JSON.stringify({ id: taskId }) });
            if (!response.ok) throw new Error();
            const result = await response.json();

            if (result.newTotalPoints !== undefined) {
                const nav = document.getElementById('nav-points-display');
                if (nav) { nav.innerText = result.newTotalPoints; nav.style.color = '#10b981'; setTimeout(() => nav.style.color = '', 500); }
            }

            updateProgressUI(cardCol, result.completedCount, result.howMany, result.isCompleted);

            if (result.isCompleted) {
                const dateDisplay = cardCol.querySelector('.date-display');
                if (dateDisplay && result.completedAt) {
                    dateDisplay.lastElementChild.className = 'text-success completion-date';
                    dateDisplay.lastElementChild.innerHTML = `<i class="bi bi-check2-circle me-1"></i>${result.completedAt}`;
                }

                // FADE TO GREEN EFFECT
                btn.classList.remove('btn-primary', 'btn-outline-danger');
                btn.classList.add('btn-success');
                btn.style.borderColor = "";

                moveCardToCompleted(cardCol);
                document.getElementById('toast-message').textContent = "Task completed!";
            } else {
                btn.disabled = false; btn.innerHTML = originalText;
                delete cardCol.dataset.isMoving;
                document.getElementById('toast-message').textContent = `Saved (${result.completedCount}/${result.howMany})`;
            }
            lastModifiedTaskId = taskId; undoToast.show();
        } catch (e) {
            console.error(e); btn.disabled = false; btn.innerHTML = originalText; delete cardCol.dataset.isMoving;
        }
    }

    async function revertTask(taskId) {
        const cardCol = document.getElementById(`task-card-${taskId}`);
        if (!cardCol) return;

        const points = cardCol.dataset.points || 0;
        const isMidAnimation = cardCol.dataset.isMoving === "to-completed";
        if (isMidAnimation) {
            cardCol.dataset.isMoving = "cancelled";
            cardCol.style.opacity = '1';
            cardCol.style.transform = 'scale(1)';
        }

        try {
            const response = await fetch(revertUrl, { method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token }, body: JSON.stringify({ id: taskId }) });
            if (!response.ok) throw new Error();
            const result = await response.json();

            if (result.newTotalPoints !== undefined) {
                const nav = document.getElementById('nav-points-display');
                if (nav) { nav.innerText = result.newTotalPoints; nav.style.color = '#ef4444'; setTimeout(() => nav.style.color = '', 500); }
            }

            const wasFullyCompleted = completedList.contains(cardCol);
            if (wasFullyCompleted || isMidAnimation) {
                moveCardToActive(cardCol, wasFullyCompleted);
            }

            updateProgressUI(cardCol, result.completedCount, result.howMany, false);

            // Restore complex button structure
            const btn = cardCol.querySelector('.btn-complete');
            const btnContainer = cardCol.querySelector('.button-container');
            if (btnContainer) {
                const isOverdue = cardCol.dataset.isOverdue === 'true';
                const btnClass = isOverdue ? "btn-outline-danger" : "btn-primary";
                btnContainer.innerHTML = getCheckInButtonHtml(btnClass, points);
            }

        } catch (e) { console.error(e); }
    }

    function updateProgressUI(cardCol, current, total, isCompleted) {
        const badge = cardCol.querySelector('.completed-label');
        const bar = cardCol.querySelector('.progress-bar');
        const progressClass = isCompleted ? 'bg-success' : 'bg-primary-theme';

        if (badge) {
            badge.textContent = `${current}/${total}`;
            badge.classList.remove('bg-success', 'bg-primary-theme');
            badge.classList.add(progressClass);
        }

        if (bar) {
            bar.style.width = `${(current / total) * 100}%`;
            if (isCompleted) {
                bar.classList.remove('progress-bar-striped', 'progress-bar-animated', 'bg-primary-theme', 'bg-danger');
                bar.classList.add('bg-success');
            } else {
                bar.classList.add('progress-bar-striped', 'progress-bar-animated');
                bar.classList.remove('bg-success');
                bar.classList.add('bg-primary-theme');
            }
        }
    }

    function moveCardToCompleted(cardCol) {
        cardCol.style.opacity = '0'; cardCol.style.transform = 'scale(0.9)';
        const points = cardCol.dataset.points || 0;

        setTimeout(() => {
            if (cardCol.dataset.isMoving === "cancelled") { delete cardCol.dataset.isMoving; return; }

            completedList.appendChild(cardCol);
            const btnC = cardCol.querySelector('.button-container');
            if (btnC) {
                btnC.innerHTML = '<div class="text-center py-2"><span class="text-success fw-bold"><i class="bi bi-check2-all me-1"></i>Completed</span></div>';
            }

            const card = cardCol.querySelector('.card');
            if (card) {
                card.classList.remove('shadow-sm');
                card.classList.remove('border-danger', 'border-warning', 'border-2');
            }

            updateGlobalCounts(1);
            completedNavItem.classList.remove('d-none');
            cardCol.style.opacity = '1'; cardCol.style.transform = 'scale(1)';
            checkEmptyState();
            delete cardCol.dataset.isMoving;
        }, 300);
    }

    function moveCardToActive(cardCol, shouldUpdateCounts) {
        cardCol.style.opacity = '0';
        const frequency = cardCol.dataset.frequency;
        const isOverdue = cardCol.dataset.isOverdue === 'true';
        const isUrgent = cardCol.dataset.isUrgent === 'true';
        const deadlineText = cardCol.dataset.deadlineText;
        const points = cardCol.dataset.points || 0;

        setTimeout(() => {
            try {
                insertCardSorted(cardCol);

                const btnC = cardCol.querySelector('.button-container');
                if (btnC) {
                    const btnClass = isOverdue ? "btn-outline-danger" : "btn-primary";
                    // Use helper to restore complex button
                    btnC.innerHTML = getCheckInButtonHtml(btnClass, points);
                }

                const dateDisplay = cardCol.querySelector('.date-display');
                if (dateDisplay) {
                    const dateSpan = dateDisplay.lastElementChild;
                    let icon = 'bi-calendar';
                    let text = 'Active';
                    let className = '';

                    if (frequency === 'Daily') { text = 'Ends Tonight'; icon = 'bi-sun'; }
                    else if (frequency === 'Weekly') { text = 'Ends Sunday'; icon = 'bi-calendar-week'; className = isUrgent ? 'text-warning fw-bold' : ''; }
                    else if (frequency === 'OneTime') {
                        text = deadlineText || 'Upcoming';
                        icon = 'bi-calendar-event';
                        if (isOverdue) {
                            text = 'Overdue: ' + text;
                            icon = 'bi-exclamation-triangle-fill';
                            className = 'text-danger fw-bold';
                        } else if (isUrgent) {
                            className = 'text-warning fw-bold';
                        }
                    }
                    dateSpan.className = className;
                    dateSpan.innerHTML = `<i class="bi ${icon} me-1"></i>${text}`;
                }

                const card = cardCol.querySelector('.card');
                let borderClass = '';
                if (isOverdue) borderClass = 'border-danger border-2';
                else if (isUrgent) borderClass = 'border-warning border-2';

                if (card) {
                    // Reset classes
                    card.className = `card h-100 shadow-sm ${borderClass}`;
                }

                if (shouldUpdateCounts) updateGlobalCounts(-1);
                checkEmptyState();

            } catch (err) { console.error(err); }
            finally {
                cardCol.style.opacity = '1';
                cardCol.style.transform = 'scale(1)';
                delete cardCol.dataset.isMoving;
            }
        }, 300);
    }

    function insertCardSorted(card) {
        const sortMode = document.getElementById('sortOrderSelect').value;
        const children = Array.from(activeList.children);
        if (children.length === 0) { activeList.appendChild(card); return; }

        const cPrio = parseInt(card.dataset.priority) || 4;
        const cTime = parseInt(card.dataset.deadlineTicks) || 0;
        const cTitle = (card.dataset.title || "").toLowerCase();

        let inserted = false;
        for (let child of children) {
            if (child === card) continue;
            const oPrio = parseInt(child.dataset.priority) || 4;
            const oTime = parseInt(child.dataset.deadlineTicks) || 0;
            const oTitle = (child.dataset.title || "").toLowerCase();

            let shouldInsert = false;
            if (sortMode === 'title') { if (cTitle < oTitle) shouldInsert = true; }
            else { if (cPrio < oPrio) shouldInsert = true; else if (cPrio === oPrio && cTime < oTime) shouldInsert = true; }

            if (shouldInsert) { activeList.insertBefore(card, child); inserted = true; break; }
        }
        if (!inserted) activeList.appendChild(card);
    }

    function updateGlobalCounts(changeToCompleted) {
        const currentActive = parseInt(activeCountEl.innerText) || 0;
        const currentCompleted = parseInt(completedCountEl.innerText) || 0;
        activeCountEl.innerText = Math.max(0, currentActive - changeToCompleted);
        completedCountEl.innerText = Math.max(0, currentCompleted + changeToCompleted);
    }

    function checkEmptyState() {
        const a = parseInt(activeCountEl.innerText) || 0;
        const c = parseInt(completedCountEl.innerText) || 0;
        activeEmptyState.className = a === 0 ? 'text-center py-5' : 'd-none';
        if (c === 0) {
            completedNavItem.classList.add('d-none');
            if (document.getElementById('completed-tab').classList.contains('active')) {
                const triggerEl = document.getElementById('active-tab');
                if (triggerEl) new bootstrap.Tab(triggerEl).show();
            }
        } else { completedNavItem.classList.remove('d-none'); }
    }
});