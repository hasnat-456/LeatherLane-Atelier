const fs = require('fs');

const igSection = `
    <!-- Instagram Section -->
    <section class="instagram-section" style="padding: 4rem 2rem; text-align: center; background: #fff;">
        <h2 style="font-family: 'Playfair Display', serif; color: #4A3B32; font-size: 2rem; margin-bottom: 0.5rem;">Follow Us On Instagram</h2>
        <p style="color: #666; margin-bottom: 2rem;"><a href="https://www.instagram.com/leatherlane_atelier" target="_blank" style="color: #C79A52; text-decoration: none; font-weight: bold;">@leatherlane_atelier</a></p>
        
        <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 15px; max-width: 1200px; margin: 0 auto;">
            <a href="https://www.instagram.com/leatherlane_atelier" target="_blank" style="display: block; overflow: hidden; border-radius: 8px;">
                <img src="/images/products/placeholder1.jpg" alt="Instagram Post" style="width: 100%; height: 250px; object-fit: cover; transition: transform 0.3s;" onmouseover="this.style.transform='scale(1.05)'" onmouseout="this.style.transform='scale(1)'" onerror="this.src='https://via.placeholder.com/250x250/f4f4f4/cccccc?text=Instagram+Post'">
            </a>
            <a href="https://www.instagram.com/leatherlane_atelier" target="_blank" style="display: block; overflow: hidden; border-radius: 8px;">
                <img src="/images/products/placeholder2.jpg" alt="Instagram Post" style="width: 100%; height: 250px; object-fit: cover; transition: transform 0.3s;" onmouseover="this.style.transform='scale(1.05)'" onmouseout="this.style.transform='scale(1)'" onerror="this.src='https://via.placeholder.com/250x250/f4f4f4/cccccc?text=Instagram+Post'">
            </a>
            <a href="https://www.instagram.com/leatherlane_atelier" target="_blank" style="display: block; overflow: hidden; border-radius: 8px;">
                <img src="/images/products/placeholder3.jpg" alt="Instagram Post" style="width: 100%; height: 250px; object-fit: cover; transition: transform 0.3s;" onmouseover="this.style.transform='scale(1.05)'" onmouseout="this.style.transform='scale(1)'" onerror="this.src='https://via.placeholder.com/250x250/f4f4f4/cccccc?text=Instagram+Post'">
            </a>
            <a href="https://www.instagram.com/leatherlane_atelier" target="_blank" style="display: block; overflow: hidden; border-radius: 8px; display: none; @media(min-width: 768px){display: block;}">
                <img src="/images/products/placeholder4.jpg" alt="Instagram Post" style="width: 100%; height: 250px; object-fit: cover; transition: transform 0.3s;" onmouseover="this.style.transform='scale(1.05)'" onmouseout="this.style.transform='scale(1)'" onerror="this.src='https://via.placeholder.com/250x250/f4f4f4/cccccc?text=Instagram+Post'">
            </a>
        </div>
    </section>
`;

let code = fs.readFileSync('front/home.html', 'utf8');
if (!code.includes('instagram-section')) {
    // Inject right before the footer
    code = code.replace('<footer', igSection + '\n    <footer');
    fs.writeFileSync('front/home.html', code, 'utf8');
    console.log("Injected Instagram section into home.html");
}
