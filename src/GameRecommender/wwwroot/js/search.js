document.addEventListener('DOMContentLoaded', () => {
    const searchInput = document.getElementById('searchInput');
    const gameCards = document.querySelectorAll('.card');
    
    searchInput.addEventListener('input', (e) => {
        const searchTerm = e.target.value.toLowerCase();
        
        gameCards.forEach(card => {
            const title = card.querySelector('.card-title').textContent.toLowerCase();
            const genre = card.querySelector('.badge').textContent.toLowerCase();
            const isVisible = title.includes(searchTerm) || genre.includes(searchTerm);
            
            card.closest('.col').style.display = isVisible ? '' : 'none';
            card.style.opacity = isVisible ? '1' : '0';
            card.style.transform = isVisible ? 'translateY(0)' : 'translateY(20px)';
        });
    });
}); 