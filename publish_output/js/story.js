document.addEventListener('DOMContentLoaded', async () => {
    const params = new URLSearchParams(window.location.search);
    const id = params.get('id');
    const container = document.getElementById('storyContainer');
    
    if (!id) {
        container.innerHTML = '<div style="text-align: center; padding: 5rem 0;">Story not found.</div>';
        return;
    }
    
    try {
        const res = await fetch(`/api/blogs/${id}`);
        if (!res.ok) throw new Error('Story not found');
        const story = await res.json();
        
        let dateStr = new Date(story.createdAt).toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' });
        let authorStr = story.author || 'LeatherLane';
        
        let html = `
            <div class="story-category">${story.category || 'Journal'}</div>
            <h1 class="story-title">${story.title}</h1>
            <div class="story-meta">${dateStr} &middot; ${authorStr}</div>
        `;
        
        if (story.image) {
            html += `<img src="${story.image}" alt="${story.title}" class="story-hero-img">`;
        }
        
        if (story.excerpt) {
            html += `<div style="font-size: 1.3rem; font-style: italic; color: #555; margin-bottom: 2rem; text-align: center; line-height: 1.6;">${story.excerpt}</div>`;
        }
        
        html += `
            <div class="story-content">
                ${story.content}
            </div>
        `;
        
        container.innerHTML = html;
        document.title = `${story.title} | LeatherLane Atelier`;
        
    } catch (error) {
        console.error(error);
        container.innerHTML = '<div style="text-align: center; padding: 5rem 0;">Story not found.</div>';
    }
});
