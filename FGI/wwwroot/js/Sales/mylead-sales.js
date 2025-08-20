document.querySelectorAll('.tab').forEach(tab => {
    tab.addEventListener('click', () => {
        document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
        tab.classList.add('active');

        filterLeads();
    });
});

function filterLeads() {
    const statusFilter = document.getElementById('statusFilter').value;
    const projectFilter = document.getElementById('projectFilter').value;
    const searchTerm = document.getElementById('searchInput').value.toLowerCase();
    const activeTabStatus = document.querySelector('.tab.active').getAttribute('data-status');

    const effectiveStatusFilter = activeTabStatus !== 'all' ? activeTabStatus : statusFilter;

    document.querySelectorAll('.lead-card').forEach(card => {
        const cardStatus = card.getAttribute('data-status');
        const cardProject = card.getAttribute('data-project');
        const cardSearch = card.getAttribute('data-search').toLowerCase();

        const statusMatch = effectiveStatusFilter === 'all' || cardStatus === effectiveStatusFilter;
        const projectMatch = projectFilter === 'all' || cardProject === projectFilter;
        const searchMatch = searchTerm === '' || cardSearch.includes(searchTerm);

        if (statusMatch && projectMatch && searchMatch) {
            card.style.display = 'flex';
        } else {
            card.style.display = 'none';
        }
    });
}

document.getElementById('statusFilter').addEventListener('change', filterLeads);
document.getElementById('projectFilter').addEventListener('change', filterLeads);
document.getElementById('searchInput').addEventListener('input', filterLeads);
document.getElementById('resetFilters').addEventListener('click', () => {
    document.getElementById('statusFilter').value = 'all';
    document.getElementById('projectFilter').value = 'all';
    document.getElementById('searchInput').value = '';

    document.querySelectorAll('.tab').forEach(tab => tab.classList.remove('active'));
    document.querySelector('.tab[data-status="all"]').classList.add('active');

    filterLeads();
});

document.querySelectorAll('.lead-card').forEach((card, index) => {
    card.style.animationDelay = `${ index * 0.1 } s`;
});
