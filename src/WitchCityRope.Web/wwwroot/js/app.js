// Witch City Rope - Main JavaScript

// Scroll listener for header
window.addScrollListener = (dotNetHelper) => {
    let lastScrollTop = 0;
    let ticking = false;
    
    function updateScrollState() {
        const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
        const scrolled = scrollTop > 50;
        
        dotNetHelper.invokeMethodAsync('OnScroll', scrolled);
        
        lastScrollTop = scrollTop;
        ticking = false;
    }
    
    function requestTick() {
        if (!ticking) {
            window.requestAnimationFrame(updateScrollState);
            ticking = true;
        }
    }
    
    window.addEventListener('scroll', requestTick);
};

// Click outside listener for dropdowns
window.addClickOutsideListener = (dotNetHelper) => {
    document.addEventListener('click', (event) => {
        const userMenu = document.querySelector('.user-menu');
        const userMenuBtn = document.querySelector('.user-menu-btn');
        
        if (userMenu && !userMenu.contains(event.target)) {
            dotNetHelper.invokeMethodAsync('OnClickOutside');
        }
    });
};

// Copy to clipboard functionality
window.copyToClipboard = (text) => {
    navigator.clipboard.writeText(text).then(() => {
        console.log('Copied to clipboard');
    }).catch(err => {
        console.error('Failed to copy: ', err);
    });
};

// Initialize tooltips
window.initializeTooltips = () => {
    const tooltips = document.querySelectorAll('[data-tooltip]');
    tooltips.forEach(element => {
        element.addEventListener('mouseenter', showTooltip);
        element.addEventListener('mouseleave', hideTooltip);
    });
};

function showTooltip(e) {
    const tooltip = document.createElement('div');
    tooltip.className = 'tooltip';
    tooltip.textContent = e.target.getAttribute('data-tooltip');
    document.body.appendChild(tooltip);
    
    const rect = e.target.getBoundingClientRect();
    tooltip.style.top = rect.top - tooltip.offsetHeight - 5 + 'px';
    tooltip.style.left = rect.left + (rect.width - tooltip.offsetWidth) / 2 + 'px';
    tooltip.classList.add('show');
    
    e.target._tooltip = tooltip;
}

function hideTooltip(e) {
    if (e.target._tooltip) {
        e.target._tooltip.remove();
        delete e.target._tooltip;
    }
}

// Auto-resize textareas
window.autoResizeTextarea = (textarea) => {
    textarea.style.height = 'auto';
    textarea.style.height = textarea.scrollHeight + 'px';
};

// Initialize auto-resize for all textareas
document.addEventListener('DOMContentLoaded', () => {
    const textareas = document.querySelectorAll('textarea[data-autoresize]');
    textareas.forEach(textarea => {
        textarea.addEventListener('input', () => window.autoResizeTextarea(textarea));
        window.autoResizeTextarea(textarea);
    });
});

// Focus trap for modals
window.trapFocus = (element) => {
    const focusableElements = element.querySelectorAll(
        'a[href], button, textarea, input[type="text"], input[type="radio"], input[type="checkbox"], select'
    );
    const firstFocusableElement = focusableElements[0];
    const lastFocusableElement = focusableElements[focusableElements.length - 1];

    element.addEventListener('keydown', (e) => {
        if (e.key === 'Tab') {
            if (e.shiftKey) {
                if (document.activeElement === firstFocusableElement) {
                    lastFocusableElement.focus();
                    e.preventDefault();
                }
            } else {
                if (document.activeElement === lastFocusableElement) {
                    firstFocusableElement.focus();
                    e.preventDefault();
                }
            }
        }
        
        if (e.key === 'Escape') {
            element.dispatchEvent(new CustomEvent('modal-close'));
        }
    });

    firstFocusableElement?.focus();
};

// Smooth scroll to element
window.smoothScrollTo = (elementId) => {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollIntoView({
            behavior: 'smooth',
            block: 'start'
        });
    }
};

// Format currency
window.formatCurrency = (amount) => {
    return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD'
    }).format(amount);
};

// Format date
window.formatDate = (date, format = 'short') => {
    const options = format === 'short' 
        ? { month: 'short', day: 'numeric' }
        : { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' };
    
    return new Date(date).toLocaleDateString('en-US', options);
};

// Print functionality
window.printElement = (elementId) => {
    const element = document.getElementById(elementId);
    if (!element) return;
    
    const printWindow = window.open('', '', 'height=600,width=800');
    printWindow.document.write('<html><head><title>Print</title>');
    printWindow.document.write('<link rel="stylesheet" href="/css/wcr-theme.css">');
    printWindow.document.write('<link rel="stylesheet" href="/css/print.css">');
    printWindow.document.write('</head><body>');
    printWindow.document.write(element.innerHTML);
    printWindow.document.write('</body></html>');
    printWindow.document.close();
    
    printWindow.onload = () => {
        printWindow.print();
        printWindow.close();
    };
};

// Download file functionality
window.downloadFile = (filename, base64Content) => {
    const byteCharacters = atob(base64Content);
    const byteNumbers = new Array(byteCharacters.length);
    
    for (let i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    
    const byteArray = new Uint8Array(byteNumbers);
    const blob = new Blob([byteArray], { type: 'text/plain' });
    
    const link = document.createElement('a');
    link.href = window.URL.createObjectURL(blob);
    link.download = filename;
    link.click();
    
    // Clean up
    setTimeout(() => {
        window.URL.revokeObjectURL(link.href);
    }, 100);
};