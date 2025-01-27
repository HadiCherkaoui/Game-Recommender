document.addEventListener('DOMContentLoaded', () => {
    const themeToggle = document.getElementById('themeToggle');
    const icon = themeToggle.querySelector('i');
    const root = document.documentElement;
    
    function setTheme(isDark) {
        if (isDark) {
            root.style.setProperty('--background', 'var(--background-dark)');
            root.style.setProperty('--text', 'var(--text-dark)');
            root.style.setProperty('--primary', 'var(--primary-dark)');
            root.style.setProperty('--secondary', 'var(--secondary-dark)');
            root.style.setProperty('--border', 'var(--border-dark)');
            icon.className = 'bi bi-sun';
        } else {
            root.style.setProperty('--background', 'var(--background-light)');
            root.style.setProperty('--text', 'var(--text-light)');
            root.style.setProperty('--primary', 'var(--primary-light)');
            root.style.setProperty('--secondary', 'var(--secondary-light)');
            root.style.setProperty('--border', 'var(--border-light)');
            icon.className = 'bi bi-moon-stars';
        }
        localStorage.setItem('theme', isDark ? 'dark' : 'light');
    }

    // Check initial theme
    const savedTheme = localStorage.getItem('theme') || 'auto';
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    setTheme(savedTheme === 'dark' || (savedTheme === 'auto' && prefersDark));

    // Toggle theme on click
    themeToggle.addEventListener('click', () => {
        const isDark = localStorage.getItem('theme') !== 'dark';
        setTheme(isDark);
    });
}); 