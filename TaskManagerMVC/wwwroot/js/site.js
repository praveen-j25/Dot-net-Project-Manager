// ============================================
// Task Manager Pro - JavaScript Validation
// ============================================

(function () {
    'use strict';

    // ============================================
    // Validation Rules
    // ============================================
    const ValidationRules = {
        required: (value) => value && value.trim().length > 0,
        email: (value) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value),
        minLength: (value, min) => value && value.length >= min,
        maxLength: (value, max) => value && value.length <= max,
        password: (value) => /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/.test(value),
        phone: (value) => !value || /^[\d\s\-\+\(\)]{10,}$/.test(value),
        match: (value, matchValue) => value === matchValue,
        date: (value) => !value || !isNaN(Date.parse(value)),
        futureDate: (value) => {
            if (!value) return true;
            const inputDate = new Date(value);
            const today = new Date();
            today.setHours(0, 0, 0, 0);
            return inputDate >= today;
        }
    };

    // ============================================
    // Error Messages
    // ============================================
    const ErrorMessages = {
        required: 'This field is required',
        email: 'Please enter a valid email address',
        minLength: (min) => `Minimum ${min} characters required`,
        maxLength: (max) => `Maximum ${max} characters allowed`,
        password: 'Password must be at least 8 characters with uppercase, lowercase, number and special character',
        phone: 'Please enter a valid phone number',
        match: 'Passwords do not match',
        date: 'Please enter a valid date',
        futureDate: 'Date must be today or in the future'
    };

    // ============================================
    // Form Validator Class
    // ============================================
    class FormValidator {
        constructor(form, config) {
            this.form = form;
            this.config = config;
            this.errors = {};
            this.init();
        }

        init() {
            // Real-time validation on input
            Object.keys(this.config).forEach(fieldName => {
                const field = this.form.querySelector(`[name="${fieldName}"]`);
                if (field) {
                    field.addEventListener('input', () => this.validateField(fieldName));
                    field.addEventListener('blur', () => this.validateField(fieldName));
                }
            });

            // Form submit validation
            this.form.addEventListener('submit', (e) => this.handleSubmit(e));
        }

        validateField(fieldName) {
            const field = this.form.querySelector(`[name="${fieldName}"]`);
            const rules = this.config[fieldName];
            const value = field.value;
            let isValid = true;
            let errorMessage = '';

            for (const rule of rules) {
                if (rule.type === 'required' && !ValidationRules.required(value)) {
                    isValid = false;
                    errorMessage = rule.message || ErrorMessages.required;
                    break;
                }
                if (rule.type === 'email' && value && !ValidationRules.email(value)) {
                    isValid = false;
                    errorMessage = rule.message || ErrorMessages.email;
                    break;
                }
                if (rule.type === 'minLength' && !ValidationRules.minLength(value, rule.value)) {
                    isValid = false;
                    errorMessage = rule.message || ErrorMessages.minLength(rule.value);
                    break;
                }
                if (rule.type === 'password' && value && !ValidationRules.password(value)) {
                    isValid = false;
                    errorMessage = rule.message || ErrorMessages.password;
                    break;
                }
                if (rule.type === 'phone' && !ValidationRules.phone(value)) {
                    isValid = false;
                    errorMessage = rule.message || ErrorMessages.phone;
                    break;
                }
                if (rule.type === 'match') {
                    const matchField = this.form.querySelector(`[name="${rule.field}"]`);
                    if (matchField && !ValidationRules.match(value, matchField.value)) {
                        isValid = false;
                        errorMessage = rule.message || ErrorMessages.match;
                        break;
                    }
                }
                if (rule.type === 'futureDate' && !ValidationRules.futureDate(value)) {
                    isValid = false;
                    errorMessage = rule.message || ErrorMessages.futureDate;
                    break;
                }
            }

            this.showFieldValidation(field, isValid, errorMessage);
            this.errors[fieldName] = !isValid;
            return isValid;
        }

        showFieldValidation(field, isValid, errorMessage) {
            const container = field.closest('.mb-3') || field.parentElement;
            let errorElement = container.querySelector('.validation-error');

            // Remove existing states
            field.classList.remove('is-valid', 'is-invalid');

            if (!errorElement) {
                errorElement = document.createElement('div');
                errorElement.className = 'validation-error text-danger small mt-1';
                container.appendChild(errorElement);
            }

            if (isValid) {
                field.classList.add('is-valid');
                errorElement.textContent = '';
                errorElement.style.display = 'none';
            } else {
                field.classList.add('is-invalid');
                errorElement.textContent = errorMessage;
                errorElement.style.display = 'block';
            }
        }

        validateAll() {
            let isFormValid = true;
            Object.keys(this.config).forEach(fieldName => {
                if (!this.validateField(fieldName)) {
                    isFormValid = false;
                }
            });
            return isFormValid;
        }

        handleSubmit(e) {
            // Check jQuery validation first if available (Unobtrusive Validation)
            if (typeof $ !== 'undefined' && $(this.form).valid && !$(this.form).valid()) {
                // jQuery validation failed, default will be prevented by its handler
                // do NOT show loading state
                return;
            }

            if (!this.validateAll()) {
                e.preventDefault();
                this.showFormError();
                // Focus first invalid field
                const firstInvalid = this.form.querySelector('.is-invalid');
                if (firstInvalid) firstInvalid.focus();
            } else {
                this.showLoading();
            }
        }

        showFormError() {
            const existingAlert = this.form.querySelector('.validation-alert');
            if (existingAlert) existingAlert.remove();

            const alert = document.createElement('div');
            alert.className = 'alert alert-danger validation-alert mb-3 animate__animated animate__shakeX';
            alert.innerHTML = '<i class="bi bi-exclamation-circle me-2"></i>Please fix the errors below';
            this.form.prepend(alert);

            setTimeout(() => alert.remove(), 5000);
        }

        showLoading() {
            const submitBtn = this.form.querySelector('[type="submit"]');
            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Please wait...';
            }
        }
    }

    // ============================================
    // Initialize Validators on DOM Ready
    // ============================================
    document.addEventListener('DOMContentLoaded', function () {

        // Login Form Validation
        const loginForm = document.querySelector('form[action*="Login"]');
        if (loginForm && !loginForm.querySelector('[name="ResetToken"]')) {
            new FormValidator(loginForm, {
                'Email': [
                    { type: 'required', message: 'Email is required' },
                    { type: 'email', message: 'Please enter a valid email' }
                ],
                'Password': [
                    { type: 'required', message: 'Password is required' },
                    { type: 'minLength', value: 6, message: 'Password must be at least 6 characters' }
                ]
            });
        }

        // Register Form Validation
        const registerForm = document.querySelector('form[action*="Register"]');
        if (registerForm) {
            new FormValidator(registerForm, {
                'FirstName': [
                    { type: 'required', message: 'First name is required' },
                    { type: 'minLength', value: 2, message: 'Minimum 2 characters' }
                ],
                'LastName': [
                    { type: 'required', message: 'Last name is required' },
                    { type: 'minLength', value: 2, message: 'Minimum 2 characters' }
                ],
                'Email': [
                    { type: 'required', message: 'Email is required' },
                    { type: 'email', message: 'Please enter a valid email' }
                ],
                'Phone': [
                    { type: 'phone', message: 'Please enter a valid phone number' }
                ],
                'Password': [
                    { type: 'required', message: 'Password is required' },
                    { type: 'password' }
                ],
                'ConfirmPassword': [
                    { type: 'required', message: 'Please confirm your password' },
                    { type: 'match', field: 'Password', message: 'Passwords do not match' }
                ]
            });

            // Password strength indicator
            const passwordField = registerForm.querySelector('[name="Password"]');
            if (passwordField) {
                const strengthIndicator = document.createElement('div');
                strengthIndicator.className = 'password-strength mt-2';
                strengthIndicator.innerHTML = `
                    <div class="progress" style="height: 5px;">
                        <div class="progress-bar" role="progressbar" style="width: 0%"></div>
                    </div>
                    <small class="strength-text text-muted mt-1 d-block"></small>
                `;
                passwordField.parentElement.appendChild(strengthIndicator);

                passwordField.addEventListener('input', function () {
                    const strength = calculatePasswordStrength(this.value);
                    const progressBar = strengthIndicator.querySelector('.progress-bar');
                    const strengthText = strengthIndicator.querySelector('.strength-text');

                    progressBar.style.width = strength.percent + '%';
                    progressBar.className = 'progress-bar ' + strength.class;
                    strengthText.textContent = strength.text;
                    strengthText.className = 'strength-text small mt-1 d-block ' + strength.textClass;
                });
            }
        }

        // Task Form Validation (Create/Edit)
        const taskForm = document.querySelector('form[action*="Create"], form[action*="Edit"]');
        if (taskForm && taskForm.querySelector('[name="Title"]')) {
            new FormValidator(taskForm, {
                'Title': [
                    { type: 'required', message: 'Task title is required' },
                    { type: 'minLength', value: 3, message: 'Title must be at least 3 characters' }
                ],
                'PriorityId': [
                    { type: 'required', message: 'Please select a priority' }
                ],
                'StatusId': [
                    { type: 'required', message: 'Please select a status' }
                ],
                'DueDate': [
                    { type: 'required', message: 'Due date is required' }
                ]
            });
        }

        // Auto-hide alerts
        const alerts = document.querySelectorAll('.alert-dismissible');
        alerts.forEach(function (alert) {
            setTimeout(() => {
                if (alert && alert.parentElement) {
                    alert.style.transition = 'opacity 0.5s';
                    alert.style.opacity = '0';
                    setTimeout(() => alert.remove(), 500);
                }
            }, 5000);
        });

        // Confirm delete with custom modal
        // Confirm delete with custom modal - REMOVED to avoid double confirmation
        // document.querySelectorAll('form[action*="Delete"]').forEach(form => {
        //     form.addEventListener('submit', function (e) {
        //         e.preventDefault();
        //         if (confirm('Are you sure you want to delete this task? This action cannot be undone.')) {
        //             this.submit();
        //         }
        //     });
        // });

        // Set default due date for new tasks
        const dueDateInput = document.querySelector('input[name="DueDate"]:not([value])');
        if (dueDateInput && !dueDateInput.value) {
            const defaultDate = new Date();
            defaultDate.setDate(defaultDate.getDate() + 7);
            dueDateInput.value = defaultDate.toISOString().split('T')[0];
            dueDateInput.min = new Date().toISOString().split('T')[0];
        }

        // Add animation to cards on scroll
        const observerOptions = { threshold: 0.1 };
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('animate-in');
                }
            });
        }, observerOptions);

        document.querySelectorAll('.card').forEach(card => observer.observe(card));

        // Tooltips initialization (Bootstrap)
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(el => new bootstrap.Tooltip(el));
    });

    // ============================================
    // Password Strength Calculator
    // ============================================
    function calculatePasswordStrength(password) {
        let score = 0;

        if (password.length >= 8) score += 25;
        if (password.length >= 12) score += 10;
        if (/[a-z]/.test(password)) score += 15;
        if (/[A-Z]/.test(password)) score += 15;
        if (/\d/.test(password)) score += 15;
        if (/[@$!%*?&]/.test(password)) score += 20;

        if (score < 30) return { percent: score, class: 'bg-danger', text: 'Weak', textClass: 'text-danger' };
        if (score < 60) return { percent: score, class: 'bg-warning', text: 'Fair', textClass: 'text-warning' };
        if (score < 80) return { percent: score, class: 'bg-info', text: 'Good', textClass: 'text-info' };
        return { percent: 100, class: 'bg-success', text: 'Strong', textClass: 'text-success' };
    }

    // ============================================
    // Toast Notification
    // ============================================
    window.showToast = function (message, type = 'success') {
        const toast = document.createElement('div');
        toast.className = `toast-notification toast-${type}`;
        toast.innerHTML = `
            <i class="bi bi-${type === 'success' ? 'check-circle' : 'exclamation-circle'} me-2"></i>
            ${message}
        `;
        document.body.appendChild(toast);

        setTimeout(() => toast.classList.add('show'), 100);
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    };

    // ============================================
    // Loading Overlay
    // ============================================
    window.showLoading = function () {
        const overlay = document.createElement('div');
        overlay.className = 'loading-overlay';
        overlay.id = 'loadingOverlay';
        overlay.innerHTML = `
            <div class="spinner-border text-primary" role="status" style="width: 3rem; height: 3rem;">
                <span class="visually-hidden">Loading...</span>
            </div>
        `;
        document.body.appendChild(overlay);
    };

    window.hideLoading = function () {
        const overlay = document.getElementById('loadingOverlay');
        if (overlay) overlay.remove();
    };

})();
