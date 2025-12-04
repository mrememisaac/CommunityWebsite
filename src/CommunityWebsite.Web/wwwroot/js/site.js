/**
 * Community Hub - Site JavaScript
 * jQuery + Bootstrap 5 based functionality
 */

// API Base URL
const API_BASE = '/api';

// Auth token storage key
const TOKEN_KEY = 'authToken';
const USER_KEY = 'currentUser';

// ============================================
// Authentication Functions
// ============================================

/**
 * Check if user is logged in
 */
function isLoggedIn() {
    return localStorage.getItem(TOKEN_KEY) !== null;
}

/**
 * Get current auth token
 */
function getToken() {
    return localStorage.getItem(TOKEN_KEY);
}

/**
 * Get current user info
 */
function getCurrentUser() {
    const userJson = localStorage.getItem(USER_KEY);
    return userJson ? JSON.parse(userJson) : null;
}

/**
 * Save auth data after login
 */
function saveAuthData(token, user) {
    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    updateAuthUI();
}

/**
 * Clear auth data on logout
 */
function clearAuthData() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    updateAuthUI();
}

/**
 * Update navigation UI based on auth state
 */
function updateAuthUI() {
    const isAuthenticated = isLoggedIn();
    const user = getCurrentUser();

    if (isAuthenticated && user) {
        $('.auth-logged-out').addClass('d-none');
        $('.auth-logged-in').removeClass('d-none');
        $('#currentUsername').text(user.username || 'User');
    } else {
        $('.auth-logged-out').removeClass('d-none');
        $('.auth-logged-in').addClass('d-none');
    }
}

/**
 * Logout user
 */
function logout() {
    clearAuthData();
    showToast('Logged out successfully', 'success');

    // Redirect to home if on a protected page
    const protectedPaths = ['/users/profile', '/posts/create', '/posts/edit'];
    const currentPath = window.location.pathname.toLowerCase();

    if (protectedPaths.some(p => currentPath.startsWith(p))) {
        window.location.href = '/';
    }
}

// ============================================
// API Helper Functions
// ============================================

/**
 * Make authenticated API request
 */
function apiRequest(url, options = {}) {
    const token = getToken();

    const defaultOptions = {
        headers: {
            'Content-Type': 'application/json',
            ...(token ? { 'Authorization': `Bearer ${token}` } : {})
        }
    };

    return $.ajax({
        url: API_BASE + url,
        ...defaultOptions,
        ...options,
        headers: { ...defaultOptions.headers, ...options.headers }
    });
}

/**
 * GET request helper
 */
function apiGet(url) {
    return apiRequest(url, { method: 'GET' });
}

/**
 * POST request helper
 */
function apiPost(url, data) {
    return apiRequest(url, {
        method: 'POST',
        data: JSON.stringify(data)
    });
}

/**
 * PUT request helper
 */
function apiPut(url, data) {
    return apiRequest(url, {
        method: 'PUT',
        data: JSON.stringify(data)
    });
}

/**
 * DELETE request helper
 */
function apiDelete(url) {
    return apiRequest(url, { method: 'DELETE' });
}

// ============================================
// Toast Notifications
// ============================================

/**
 * Show toast notification
 */
function showToast(message, type = 'info') {
    // Ensure toast container exists
    let container = $('.toast-container');
    if (container.length === 0) {
        container = $('<div class="toast-container"></div>');
        $('body').append(container);
    }

    const iconMap = {
        success: 'bi-check-circle-fill text-success',
        error: 'bi-exclamation-circle-fill text-danger',
        warning: 'bi-exclamation-triangle-fill text-warning',
        info: 'bi-info-circle-fill text-primary'
    };

    const toastId = 'toast-' + Date.now();
    const toastHtml = `
        <div id="${toastId}" class="toast align-items-center border-0" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="bi ${iconMap[type] || iconMap.info} me-2"></i>
                    ${escapeHtml(message)}
                </div>
                <button type="button" class="btn-close me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>
    `;

    container.append(toastHtml);

    const toastEl = document.getElementById(toastId);
    const toast = new bootstrap.Toast(toastEl, { autohide: true, delay: 4000 });
    toast.show();

    // Remove from DOM after hidden
    toastEl.addEventListener('hidden.bs.toast', () => {
        toastEl.remove();
    });
}

// ============================================
// Utility Functions
// ============================================

/**
 * Escape HTML to prevent XSS
 */
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

/**
 * Format date for display
 */
function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });
}

/**
 * Format date with time
 */
function formatDateTime(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}

/**
 * Get relative time (e.g., "2 hours ago")
 */
function getRelativeTime(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const diffInSeconds = Math.floor((now - date) / 1000);

    if (diffInSeconds < 60) return 'just now';
    if (diffInSeconds < 3600) return Math.floor(diffInSeconds / 60) + ' min ago';
    if (diffInSeconds < 86400) return Math.floor(diffInSeconds / 3600) + ' hours ago';
    if (diffInSeconds < 604800) return Math.floor(diffInSeconds / 86400) + ' days ago';

    return formatDate(dateString);
}

/**
 * Truncate text with ellipsis
 */
function truncateText(text, maxLength) {
    if (!text || text.length <= maxLength) return text;
    return text.substring(0, maxLength).trim() + '...';
}

/**
 * Debounce function for search input
 */
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

/**
 * Get initials from username
 */
function getInitials(username) {
    if (!username) return '?';
    return username.charAt(0).toUpperCase();
}

// ============================================
// Loading States
// ============================================

/**
 * Show loading spinner in element
 */
function showLoading(selector) {
    $(selector).html(`
        <div class="text-center py-4">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
        </div>
    `);
}

/**
 * Show skeleton loading cards
 */
function showSkeletonCards(selector, count = 3) {
    let html = '<div class="row g-4">';
    for (let i = 0; i < count; i++) {
        html += `
            <div class="col-md-4">
                <div class="card">
                    <div class="card-body">
                        <div class="skeleton skeleton-title"></div>
                        <div class="skeleton skeleton-text"></div>
                        <div class="skeleton skeleton-text" style="width: 80%"></div>
                        <div class="d-flex align-items-center mt-3">
                            <div class="skeleton skeleton-avatar me-2"></div>
                            <div class="skeleton skeleton-text" style="width: 100px"></div>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }
    html += '</div>';
    $(selector).html(html);
}

// ============================================
// Form Validation
// ============================================

/**
 * Validate email format
 */
function isValidEmail(email) {
    const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return regex.test(email);
}

/**
 * Validate password strength
 */
function isValidPassword(password) {
    return password && password.length >= 8;
}

/**
 * Show form validation error
 */
function showFieldError(fieldId, message) {
    const field = $('#' + fieldId);
    field.addClass('is-invalid');

    // Add error message if not exists
    let feedback = field.siblings('.invalid-feedback');
    if (feedback.length === 0) {
        feedback = $('<div class="invalid-feedback"></div>');
        field.after(feedback);
    }
    feedback.text(message);
}

/**
 * Clear form validation errors
 */
function clearFieldErrors(formId) {
    $(`#${formId} .is-invalid`).removeClass('is-invalid');
    $(`#${formId} .invalid-feedback`).text('');
}

// ============================================
// Back to Top Button
// ============================================

function initBackToTop() {
    // Create button if not exists
    if ($('.btn-back-to-top').length === 0) {
        $('body').append(`
            <button class="btn btn-primary btn-back-to-top rounded-circle" 
                    onclick="scrollToTop()" title="Back to top">
                <i class="bi bi-arrow-up"></i>
            </button>
        `);
    }

    // Show/hide based on scroll position
    $(window).scroll(function () {
        if ($(this).scrollTop() > 300) {
            $('.btn-back-to-top').addClass('show');
        } else {
            $('.btn-back-to-top').removeClass('show');
        }
    });
}

function scrollToTop() {
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

// ============================================
// Confirmation Dialogs
// ============================================

/**
 * Show confirmation modal
 */
function confirmAction(title, message, onConfirm) {
    // Create modal if not exists
    let modal = $('#confirmModal');
    if (modal.length === 0) {
        const modalHtml = `
            <div class="modal fade" id="confirmModal" tabindex="-1" aria-hidden="true">
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title" id="confirmModalTitle"></h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <div class="modal-body" id="confirmModalBody"></div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                            <button type="button" class="btn btn-danger" id="confirmModalBtn">Confirm</button>
                        </div>
                    </div>
                </div>
            </div>
        `;
        $('body').append(modalHtml);
        modal = $('#confirmModal');
    }

    $('#confirmModalTitle').text(title);
    $('#confirmModalBody').text(message);

    const confirmBtn = $('#confirmModalBtn');
    confirmBtn.off('click').on('click', function () {
        bootstrap.Modal.getInstance(modal[0]).hide();
        if (typeof onConfirm === 'function') {
            onConfirm();
        }
    });

    new bootstrap.Modal(modal[0]).show();
}

// ============================================
// Document Ready
// ============================================

$(document).ready(function () {
    // Update auth UI on page load
    updateAuthUI();

    // Initialize back to top button
    initBackToTop();

    // Auto-hide alerts after 5 seconds
    setTimeout(function () {
        $('.alert-dismissible').alert('close');
    }, 5000);

    // Add active class to current nav item
    const currentPath = window.location.pathname.toLowerCase();
    $('.navbar-nav .nav-link').each(function () {
        const href = $(this).attr('href');
        if (href && currentPath.startsWith(href.toLowerCase()) && href !== '/') {
            $(this).addClass('active');
        } else if (href === '/' && currentPath === '/') {
            $(this).addClass('active');
        }
    });

    // Form validation on submit
    $('form[data-validate]').on('submit', function (e) {
        const form = $(this);
        if (!form[0].checkValidity()) {
            e.preventDefault();
            e.stopPropagation();
        }
        form.addClass('was-validated');
    });

    // Character counter for textareas
    $('textarea[data-max-length]').each(function () {
        const textarea = $(this);
        const maxLength = parseInt(textarea.data('max-length'));

        const counter = $(`<small class="text-muted float-end"><span class="current">0</span>/${maxLength}</small>`);
        textarea.after(counter);

        textarea.on('input', function () {
            const current = textarea.val().length;
            counter.find('.current').text(current);

            if (current > maxLength) {
                counter.addClass('text-danger').removeClass('text-muted');
            } else {
                counter.addClass('text-muted').removeClass('text-danger');
            }
        });
    });

    console.log('Community Hub initialized');
});
