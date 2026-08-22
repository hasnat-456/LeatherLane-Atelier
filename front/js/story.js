document.addEventListener('DOMContentLoaded', async () => {
    const params = new URLSearchParams(window.location.search);
    const id = params.get('id');
    const container = document.getElementById('storyContainer');
    
    if (!id) {
        document.title = "The Atelier Journal | LeatherLane";
        try {
            const res = await fetch('/api/blogs');
            if (!res.ok) throw new Error('Failed to fetch stories');
            const stories = await res.json();
            
            if (stories.length === 0) {
                container.innerHTML = '<div style="text-align: center; padding: 5rem 0;">No stories published yet.</div>';
                return;
            }
            
            let html = '<div style="display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 2rem;">';
            
            stories.forEach(story => {
                html += `
                    <div style="background: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.05); transition: transform 0.3s; cursor: pointer;" onmouseover="this.style.transform='translateY(-5px)'" onmouseout="this.style.transform='translateY(0)'" onclick="window.location.href='/story?id=${story.id}'">
                        <img src="${story.image || 'https://via.placeholder.com/400x250'}" style="width: 100%; height: 250px; object-fit: cover;">
                        <div style="padding: 1.5rem;">
                            <div style="color: var(--primary-gold); font-size: 0.8rem; font-weight: 600; text-transform: uppercase; margin-bottom: 0.5rem;">${story.category || 'Journal'}</div>
                            <h4 style="font-family: var(--font-heading); color: var(--primary-bg); font-size: 1.4rem; margin: 0 0 1rem 0;">${story.title}</h4>
                            <p style="color: #666; font-size: 0.95rem; line-height: 1.5; margin: 0;">${story.excerpt || ''}</p>
                        </div>
                    </div>
                `;
            });
            html += '</div>';
            container.innerHTML = html;
        } catch(e) {
            console.error(e);
            container.innerHTML = '<div style="text-align: center; padding: 5rem 0;">Failed to load stories.</div>';
        }
        return;
    }
    
    try {
        const res = await fetch(`/api/blogs/${id}`);
        if (!res.ok) throw new Error('Story not found');
        const story = await res.json();
        
        let dateStr = new Date(story.createdAt).toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' });
        let authorStr = story.author || 'LeatherLane';
        
        // 1. Update the Hero Section
        const heroSection = document.querySelector('.hero');
        if (heroSection) {
            heroSection.style.minHeight = '60vh'; // Not fully 100vh so users know to scroll
            
            if (story.image) {
                // Remove the default slider and set the background directly
                const heroSlider = document.querySelector('.hero-slider');
                if (heroSlider) heroSlider.remove();
                
                heroSection.style.backgroundImage = `linear-gradient(rgba(0,0,0,0.6), rgba(0,0,0,0.6)), url('${story.image}')`;
                heroSection.style.backgroundSize = 'cover';
                heroSection.style.backgroundPosition = 'center center';
                heroSection.style.backgroundRepeat = 'no-repeat';
            }
            
            const heroContent = document.querySelector('.hero-content');
            if (heroContent) {
                heroContent.innerHTML = `
                    <div style="text-transform: uppercase; letter-spacing: 3px; font-size: 0.9rem; color: var(--primary-gold); margin-bottom: 1rem; text-shadow: 0 2px 4px rgba(0,0,0,0.8);">${story.category || 'Journal'}</div>
                    <h1 style="font-family: var(--font-heading); font-size: 3.5rem; color: #fff; margin-bottom: 1rem; text-transform: uppercase; letter-spacing: 2px; text-shadow: 0 4px 10px rgba(0,0,0,0.6);">${story.title}</h1>
                    <div style="font-size: 1rem; color: #ddd; letter-spacing: 1px; text-transform: uppercase; text-shadow: 0 2px 4px rgba(0,0,0,0.8);">${dateStr} &middot; ${authorStr}</div>
                `;
            }
        }
        
        // 2. Render the actual content
        let html = '';
        
        if (story.excerpt) {
            html += `
                <div style="font-size: 1.4rem; font-family: var(--font-heading); color: var(--primary-bg); margin-bottom: 3rem; text-align: center; line-height: 1.6; border-bottom: 1px solid #eaeaea; padding-bottom: 2rem;">
                    "${story.excerpt}"
                </div>
            `;
        }
        
        html += `
            <div class="story-content" style="font-size: 1.15rem; line-height: 2; color: #333;">
                ${story.content}
            </div>
        `;
        
        // Ensure images inside the content look nice and are centered
        html += `
            <style>
                .story-content img {
                    border-radius: 8px;
                    box-shadow: 0 10px 30px rgba(0,0,0,0.1);
                    margin: 3rem auto;
                    display: block;
                    max-width: 100%;
                }
                .story-content p {
                    margin-bottom: 2rem;
                }
            </style>
        `;
        
        container.innerHTML = html;
        document.title = `${story.title} | LeatherLane Atelier`;
        
    } catch (error) {
        console.error(error);
        container.innerHTML = '<div style="text-align: center; padding: 5rem 0;">Story not found.</div>';
    }
});
