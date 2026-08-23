
        async function loadFeaturedProducts() {
            try {
                // Fetch categories and all products simultaneously
                const [catRes, prodRes] = await Promise.all([
                    fetch('/api/products/categories'),
                    fetch('/api/products') // Fetch all to get new arrivals and categories
                ]);
                
                if (catRes.ok && prodRes.ok) {
                    const categories = await catRes.json();
                    const allProducts = await prodRes.json(); // Ordered by CreatedAt desc by default from API
                    
                    const container = document.getElementById('dynamicFeaturedCategories');
                    let html = '';

                    // 1. Render "New Arrivals" Section
                    if (allProducts && allProducts.length > 0) {
                        html += `
                        <section class="products-section" style="padding: 4rem 2rem; background-color: #fff; border-bottom: 1px solid #eaeaea;">
                            <div style="display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: 2rem; max-width: 1200px; margin-left: auto; margin-right: auto; padding: 0 1rem;">
                                <h3 style="font-family: var(--font-heading); color: var(--primary-bg); font-size: 2.2rem; text-transform: uppercase; margin: 0; border-bottom: 2px solid var(--primary-gold); padding-bottom: 5px;">New Arrivals</h3>
                                ${allProducts.length > 4 ? `<a href="/products" style="color: var(--primary-gold); font-weight: 600; text-decoration: none; font-size: 1rem; transition: opacity 0.2s;">Shop All New Arrivals &rarr;</a>` : ''}
                            </div>
                            <div class="product-grid" style="margin-bottom: 2rem;">
                        `;

                        const topNew = allProducts.slice(0, 4);
                        topNew.forEach(product => {
                            html += `
                                <div class="product-card">
                                    <a href="/product-detail?id=${product.id}" style="text-decoration: none; color: inherit;">
                                        <img src="${product.thumbnail || 'https://via.placeholder.com/300x250'}" class="product-img" loading="lazy" style="width: 100%; height: 250px; object-fit: cover;">
                                    </a>
                                    <div class="product-info">
                                        <div class="product-category">${product.category || ''}</div>
                                        <a href="/product-detail?id=${product.id}" style="text-decoration: none; color: inherit;">
                                            <div class="product-title" style="cursor: pointer; min-height: 48px;">${product.name}</div>
                                        </a>
                                        <div class="product-bottom">
                                            <div class="product-price">Rs. ${product.price.toLocaleString()}</div>
                                            <button class="btn-cart" onclick="window.location.href='/product-detail?id=${product.id}'">View</button>
                                        </div>
                                    </div>
                                </div>
                            `;
                        });

                        html += `
                            </div>
                        </section>
                        `;
                    }

                    // Group all products by category name
                    const groupedProducts = {};
                    if (allProducts && allProducts.length > 0) {
                        allProducts.forEach(p => {
                            const catName = p.category || 'Other';
                            if (!groupedProducts[catName]) groupedProducts[catName] = [];
                            groupedProducts[catName].push(p);
                        });
                    }

                    // 2. Loop through ALL active categories to render their sections
                    categories.forEach((cat, index) => {
                        const categoryName = cat.name;
                        const items = groupedProducts[categoryName] || [];
                        
                        if (items.length === 0) {
                            return; // Skip categories with no products
                        }

                        // Alternate background color for visual distinction
                        const bgColor = index % 2 === 0 ? 'var(--light-bg)' : '#fff';

                        html += `
                        <section class="products-section" style="padding: 4rem 2rem; background-color: ${bgColor}; border-bottom: 1px solid #eaeaea;">
                            <div style="display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: 2rem; max-width: 1200px; margin-left: auto; margin-right: auto; padding: 0 1rem;">
                                <h3 style="font-family: var(--font-heading); color: var(--primary-bg); font-size: 2.2rem; text-transform: uppercase; margin: 0; border-bottom: 2px solid var(--primary-gold); padding-bottom: 5px;">${categoryName}</h3>
                                ${items.length > 4 ? `<a href="/products?category=${encodeURIComponent(categoryName)}" style="color: var(--primary-gold); font-weight: 600; text-decoration: none; font-size: 1rem; transition: opacity 0.2s;">See More in ${categoryName} &rarr;</a>` : ''}
                            </div>
                            <div class="product-grid" style="margin-bottom: 2rem;">
                        `;

                        // Render up to 4 products for this category
                        const topItems = items.slice(0, 4);
                        topItems.forEach(product => {
                            html += `
                                <div class="product-card">
                                    <a href="/product-detail?id=${product.id}" style="text-decoration: none; color: inherit;">
                                        <img src="${product.thumbnail || 'https://via.placeholder.com/300x250'}" class="product-img" loading="lazy" style="width: 100%; height: 250px; object-fit: cover;">
                                    </a>
                                    <div class="product-info">
                                        <div class="product-category">${product.category || ''}</div>
                                        <a href="/product-detail?id=${product.id}" style="text-decoration: none; color: inherit;">
                                            <div class="product-title" style="cursor: pointer; min-height: 48px;">${product.name}</div>
                                        </a>
                                        <div class="product-bottom">
                                            <div class="product-price">Rs. ${product.price.toLocaleString()}</div>
                                            <button class="btn-cart" onclick="window.location.href='/product-detail?id=${product.id}'">View</button>
                                        </div>
                                    </div>
                                </div>
                            `;
                        });

                        html += `
                            </div>
                        </section>
                        `;
                    });

                    // Replace dummy content with dynamic content
                    container.innerHTML = html;
                }
            } catch (err) {
                console.error("Failed to load products and categories", err);
                document.getElementById('dynamicFeaturedCategories').innerHTML = '<div style="text-align: center; padding: 4rem;">Failed to load products.</div>';
            }
        }

        document.addEventListener('DOMContentLoaded', () => {
            loadFeaturedProducts();
            loadCraftSlider();
            loadHeroSlider();
            loadJournalStories();
        });

        async function loadHeroSlider() {
            try {
                const res = await fetch('/api/settings');
                if (!res.ok) return;
                const settings = await res.json();
                
                let images = [];
                try { images = JSON.parse(settings.heroSliderImages || '[]'); } catch(e) {}
                
                const wrapper = document.getElementById('heroSliderWrapper');
                if (!wrapper) return;
                
                if (images.length === 0) {
                    // Fallback default image if none exist
                    wrapper.innerHTML = `<div class="hero-slide" style="background-image: url('https://images.unsplash.com/photo-1547949003-9792a18a2601?ixlib=rb-4.0.3&auto=format&fit=crop&w=1200&q=80'); opacity: 1; animation: none;"></div>`;
                    return;
                }
                
                let slidesHtml = '';
                // Since the CSS animation 'slideAnimation' expects each to be delayed by (TotalAnimationTime / Count),
                // we calculate an animation duration (e.g. 5s per image)
                const durationPerSlide = 5; 
                const totalDuration = images.length * durationPerSlide;
                
                images.forEach((src, i) => {
                    const delay = i * durationPerSlide;
                    const urlPath = src.startsWith('/') ? src : '/' + src;
                    slidesHtml += `<div class="hero-slide" style="
                        background-image: url('${urlPath}');
                        animation: slideAnimation ${totalDuration}s infinite;
                        animation-delay: ${delay}s;
                    "></div>`;
                });
                
                wrapper.innerHTML = slidesHtml;
            } catch (err) {
                console.error(err);
            }
        }



        // ── Craft Slider State ──
        let _craftImages = [];
        let _craftIndex = 0;
        let _craftTimer = null;

        function craftSliderMove(dir) {
            if (_craftImages.length === 0) return;
            _craftIndex = (_craftIndex + dir + _craftImages.length) % _craftImages.length;
            updateCraftSlider();
            resetCraftAutoplay();
        }

        function craftSliderGoTo(idx) {
            _craftIndex = idx;
            updateCraftSlider();
            resetCraftAutoplay();
        }

        function updateCraftSlider() {
            const wrapper = document.getElementById('craftSlidesWrapper');
            const dotsEl  = document.getElementById('craftDots');
            if (!wrapper) return;

            // Move wrapper
            wrapper.style.transform = `translateX(-${(_craftIndex * 100) / _craftImages.length}%)`;

            // Update dots
            if (dotsEl) {
                Array.from(dotsEl.children).forEach((dot, i) => {
                    dot.style.background = i === _craftIndex ? '#fff' : 'rgba(255,255,255,0.4)';
                });
            }
        }

        function resetCraftAutoplay() {
            if (_craftTimer) clearInterval(_craftTimer);
            if (_craftImages.length > 1) {
                _craftTimer = setInterval(() => craftSliderMove(1), 5000);
            }
        }

        async function loadCraftSlider() {
            try {
                const res = await fetch('/api/settings');
                if (!res.ok) return;
                const settings = await res.json();

                let images = [];
                try { images = JSON.parse(settings.craftSliderImages || '[]'); } catch(e) {}

                const wrapper = document.getElementById('craftSlidesWrapper');
                const dotsEl  = document.getElementById('craftDots');
                const prevBtn = document.getElementById('craftPrev');
                const nextBtn = document.getElementById('craftNext');
                if (!wrapper) return;

                if (images.length === 0) {
                    // No images uploaded — hide arrows and dots, keep plain background
                    if (prevBtn) prevBtn.style.display = 'none';
                    if (nextBtn) nextBtn.style.display = 'none';
                    return;
                }

                _craftImages = images;
                _craftIndex = 0;

                // Build slides — each slide is 100% wide side by side
                wrapper.style.width = `${images.length * 100}%`;
                let slidesHtml = '';
                images.forEach(src => {
                    const urlPath = src.startsWith('/') ? src : '/' + src;
                    slidesHtml += `<div style="
                        flex: 0 0 ${100 / images.length}%;
                        height: 100%;
                        background-image: url('${urlPath}');
                        background-size: cover;
                        background-position: center;
                    "></div>`;
                });
                wrapper.innerHTML = slidesHtml;

                // Build dots
                if (dotsEl) {
                    dotsEl.innerHTML = '';
                    images.forEach((_, i) => {
                        const dot = document.createElement('button');
                        dot.style.cssText = `width:10px;height:10px;border-radius:50%;border:none;cursor:pointer;padding:0;background:${i===0?'#fff':'rgba(255,255,255,0.4)'}`;
                        dot.onclick = () => craftSliderGoTo(i);
                        dotsEl.appendChild(dot);
                    });
                }

                // Hide arrows if only one image
                if (images.length === 1) {
                    if (prevBtn) prevBtn.style.display = 'none';
                    if (nextBtn) nextBtn.style.display = 'none';
                }

                updateCraftSlider();
                resetCraftAutoplay();

            } catch(e) {
                console.error('Could not load craft slider', e);
            }
        }

        async function loadJournalStories() {
            try {
                const res = await fetch('/api/blogs');
                if (res.ok) {
                    const stories = await res.json();
                    const container = document.getElementById('dynamicJournalGrid');
                    if (!container) return;
                    
                    if (stories.length === 0) {
                        document.getElementById('atelierJournalSection').style.display = 'none';
                        return;
                    }
                    
                    document.getElementById('atelierJournalSection').style.display = 'block';
                    
                    let html = '';
                    const recentStories = stories.slice(0, 3);
                    recentStories.forEach(story => {
                        html += `
                            <div style="background: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.05); transition: transform 0.3s; cursor: pointer;" onmouseover="this.style.transform='translateY(-5px)'" onmouseout="this.style.transform='translateY(0)'" onclick="window.location.href='/story?id=${story.id}'">
                                <img src="${story.image || 'https://via.placeholder.com/400x250'}" style="width: 100%; height: 250px; object-fit: cover;" loading="lazy">
                                <div style="padding: 1.5rem;">
                                    <div style="color: var(--primary-gold); font-size: 0.8rem; font-weight: 600; text-transform: uppercase; margin-bottom: 0.5rem;">${story.category || 'Journal'}</div>
                                    <h4 style="font-family: var(--font-heading); color: var(--primary-bg); font-size: 1.4rem; margin: 0 0 1rem 0;">${story.title}</h4>
                                    <p style="color: #666; font-size: 0.95rem; line-height: 1.5; margin: 0;">${story.excerpt || ''}</p>
                                </div>
                            </div>
                        `;
                    });
                    container.innerHTML = html;
                }
            } catch (e) {
                console.error("Failed to load stories", e);
            }
        }
    
