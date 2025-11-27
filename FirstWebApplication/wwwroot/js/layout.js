// Layout-specific JavaScript for navigation and dark mode

// Mobile menu toggle
function toggleMobileMenu() {
    const menu = document.getElementById('mobile-menu');
    menu.classList.toggle('active');
}

// Close mobile menu when clicking on links or outside
document.addEventListener('DOMContentLoaded', function () {
    // 1. Attach event listeners to buttons (CSP-compliant)

    // Header Mobile Menu Button
    const mobileMenuButton = document.getElementById('mobile-menu-button');
    if (mobileMenuButton) {
        mobileMenuButton.addEventListener('click', toggleMobileMenu);
    }

    // Header Dark Mode Button (Desktop)
    const darkModeToggle = document.getElementById('dark-mode-toggle');
    if (darkModeToggle) {
        darkModeToggle.addEventListener('click', toggleDarkMode);
    }

    // Header Dark Mode Button (Mobile Menu)
    const mobileDarkModeToggle = document.getElementById('mobile-dark-mode-toggle');
    if (mobileDarkModeToggle) {
        mobileDarkModeToggle.addEventListener('click', toggleDarkMode);
    }

    // Settings Page Dark Mode Toggle (Den nye knappen på innstillinger-siden)
    const settingsThemeToggle = document.getElementById('settings-theme-toggle');
    if (settingsThemeToggle) {
        settingsThemeToggle.addEventListener('click', toggleDarkMode);
    }

    // 2. Close mobile menu when clicking outside
    document.addEventListener('click', function (event) {
        const menu = document.getElementById('mobile-menu');
        const button = document.getElementById('mobile-menu-button'); // Rettet selector
        const nav = document.querySelector('nav');

        // Only close if click is outside the entire nav area
        // Sjekk at vi faktisk har elementene før vi sjekker .contains
        if (menu && menu.classList.contains('active') && nav && !nav.contains(event.target)) {
            menu.classList.remove('active');
        }
    });

    // 3. Close menu when clicking a link inside it
    const menuLinks = document.querySelectorAll('#mobile-menu a, #mobile-menu button[type="submit"]');
    menuLinks.forEach(function (link) {
        link.addEventListener('click', function () {
            const menu = document.getElementById('mobile-menu');
            if (menu) {
                menu.classList.remove('active');
            }
        });
    });

    // 4. Initialize dark mode on page load
    initializeDarkMode();

    // 5. Lytt etter endringer fra andre faner (valgfritt, men god praksis)
    window.addEventListener('storage', (e) => {
        if (e.key === 'darkMode') {
            initializeDarkMode();
        }
    });
});

// Dark mode functionality
function toggleDarkMode() {
    // Tailwind bruker vanligvis 'class="dark"' på selve <html> elementet, ikke body.
    // Vi bruker document.documentElement for å være kompatibel med standard Tailwind.
    const html = document.documentElement;
    const isDark = html.classList.toggle('dark');

    // Update icons and settings switches
    updateDarkModeVisuals(isDark);

    // Save preference
    localStorage.setItem('darkMode', isDark ? 'enabled' : 'disabled');
}

function updateDarkModeVisuals(isDark) {
    // --- Oppdater Header Ikoner ---
    const sunIcon = document.getElementById('sun-icon');
    const moonIcon = document.getElementById('moon-icon');
    const mobileSunIcon = document.getElementById('mobile-sun-icon');
    const mobileMoonIcon = document.getElementById('mobile-moon-icon');
    const darkModeText = document.getElementById('dark-mode-text');

    if (isDark) {
        if (sunIcon) sunIcon.classList.remove('hidden');
        if (moonIcon) moonIcon.classList.add('hidden');
        if (mobileSunIcon) mobileSunIcon.classList.remove('hidden');
        if (mobileMoonIcon) mobileMoonIcon.classList.add('hidden');
        if (darkModeText) darkModeText.textContent = 'Light Mode';
    } else {
        if (sunIcon) sunIcon.classList.add('hidden');
        if (moonIcon) moonIcon.classList.remove('hidden');
        if (mobileSunIcon) mobileSunIcon.classList.add('hidden');
        if (mobileMoonIcon) mobileMoonIcon.classList.remove('hidden');
        if (darkModeText) darkModeText.textContent = 'Dark Mode';
    }

    // --- Oppdater Innstillinger-side Switch (hvis den finnes) ---
    const settingsToggleBtn = document.getElementById('settings-theme-toggle');
    const toggleCircle = document.getElementById('toggle-circle');

    if (settingsToggleBtn && toggleCircle) {
        if (isDark) {
            settingsToggleBtn.classList.remove('bg-gray-200');
            settingsToggleBtn.classList.add('bg-indigo-600');
            settingsToggleBtn.setAttribute('aria-checked', 'true');

            toggleCircle.classList.remove('translate-x-0');
            toggleCircle.classList.add('translate-x-5');
        } else {
            settingsToggleBtn.classList.add('bg-gray-200');
            settingsToggleBtn.classList.remove('bg-indigo-600');
            settingsToggleBtn.setAttribute('aria-checked', 'false');

            toggleCircle.classList.add('translate-x-0');
            toggleCircle.classList.remove('translate-x-5');
        }
    }
}

function initializeDarkMode() {
    const storedPreference = localStorage.getItem('darkMode');
    const html = document.documentElement;
    let isDark = false;

    if (storedPreference === 'enabled') {
        isDark = true;
    } else if (storedPreference === 'disabled') {
        isDark = false;
    } else {
        // Check system preference if no storage found
        isDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    }

    // Apply class
    if (isDark) {
        html.classList.add('dark');
    } else {
        html.classList.remove('dark');
    }

    // Update UI to match
    updateDarkModeVisuals(isDark);
}