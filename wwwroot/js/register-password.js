(function () {
    const passwordInput = document.getElementById('passwordInput');
    const confirmInput = document.getElementById('confirmPasswordInput');
    const submitBtn = document.getElementById('registerSubmit');
    const rulesList = document.getElementById('passwordRules');
    const emailInput = document.querySelector('[name="Input.Email"]');
    const progressBar = document.getElementById('passwordProgress');
    const status = document.getElementById('passwordStatus');

    if (!passwordInput || !confirmInput || !submitBtn || !rulesList) return;

    // Read rules from data attributes
    const options = {
        minLength: parseInt(passwordInput.dataset.minLength, 10) || 0,
        requireUpper: passwordInput.dataset.requireUpper === "true",
        requireLower: passwordInput.dataset.requireLower === "true",
        requireDigit: passwordInput.dataset.requireDigit === "true",
        requireSymbol: passwordInput.dataset.requireSymbol === "true",
        uniqueChars: parseInt(passwordInput.dataset.uniqueChars, 10) || 0
    };

    const rules = {
        length: p => p.length >= options.minLength,
        uppercase: p => !options.requireUpper || /[A-Z]/.test(p),
        lowercase: p => !options.requireLower || /[a-z]/.test(p),
        digit: p => !options.requireDigit || /\d/.test(p),
        symbol: p => !options.requireSymbol || /[^a-zA-Z0-9]/.test(p),
        unique: p => options.uniqueChars <= 1 || new Set(p).size >= options.uniqueChars
    };

    function validate() {
        const password = passwordInput.value;
        const confirm = confirmInput.value;

        let passed = 0;
        let total = 0;
        let firstFailure = null;

        for (const rule in rules) {
            const li = rulesList.querySelector(`[data-rule="${rule}"]`);
            if (!li) continue;

            total++;
            const ok = rules[rule](password);

            li.classList.toggle('text-success', ok);
            li.classList.toggle('text-danger', !ok);

            if (ok) passed++;
            else if (!firstFailure) firstFailure = li.textContent.trim();
        }

        const progress = total === 0 ? 0 : Math.round((passed / total) * 100);

        progressBar.style.width = `${progress}%`;
        progressBar.className =
            'progress-bar ' +
            (progress < 50 ? 'bg-danger' :
                progress < 100 ? 'bg-warning' : 'bg-success');

        if (!password) {
            status.textContent = 'Enter a password to see requirements';
        } else if (progress === 100) {
            status.textContent = 'Password meets all requirements';
        } else {
            status.textContent = `Missing: ${firstFailure}`;
        }

        const confirmValid = confirm && password === confirm;
        const emailValid = emailInput?.checkValidity();

        submitBtn.disabled = !(progress === 100 && confirmValid && emailValid);
    }

    ['input', 'change', 'keyup'].forEach(evt => {
        passwordInput.addEventListener(evt, validate);
        confirmInput.addEventListener(evt, validate);
        emailInput?.addEventListener(evt, validate);
    });

    validate();
})();
