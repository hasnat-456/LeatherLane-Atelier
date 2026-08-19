// security.js - Universal Input Validation & Anti-XSS
document.addEventListener('DOMContentLoaded', () => {
    function applySecurityToElement(input) {
        if (input.type === 'password' || input.type === 'hidden' || input.dataset.secured) return;
        if (input.dataset.skipSecurity === 'true') return; // Allow field to opt out
        input.dataset.secured = 'true';
        
        input.addEventListener('input', function(e) {
            let val = this.value;
            let originalVal = val;
            
            // 1. Anti-XSS: Remove < and > to prevent script injection globally
            val = val.replace(/[<>]/g, '');

            const name = (this.name || '').toLowerCase();
            const id = (this.id || '').toLowerCase();
            const isEmail = input.type === 'email' || name.includes('email') || id.includes('email');
            const isPassword = input.type === 'password' || name.includes('password') || id.includes('password');
            const isAddress = name.includes('address') || id.includes('address');

            if (!isEmail && !isPassword && !isAddress) {
                // 2. Number fields (Phone, ZIP, Quantities, Prices)
                const isPhoneOrMobile = name.includes('phone') || name.includes('mobile') || id.includes('phone') || id.includes('mobile');
                const isZipOrPin = name.includes('zip') || name.includes('pin') || id.includes('zip') || id.includes('pin');
                const isPriceOrQty = name.includes('price') || name.includes('qty') || name.includes('quantity') || id.includes('price') || id.includes('qty') || input.type === 'number';
                
                if (isPhoneOrMobile) {
                    val = val.replace(/[^0-9+]/g, ''); // Digits and +
                } 
                else if (isZipOrPin) {
                    val = val.replace(/[^0-9]/g, ''); // Digits only
                }
                else if (isPriceOrQty) {
                    val = val.replace(/[^0-9.]/g, ''); // Digits and dot
                }
                else {
                    // 3. Name/Text fields (Name, City, Category, etc.)
                    const isNameOrCity = name.includes('name') || name.includes('city') || id.includes('name') || id.includes('city') || id.includes('category');
                    if (isNameOrCity && !name.includes('username') && !id.includes('username')) {
                        // Allow letters, spaces, hyphens, apostrophes
                        val = val.replace(/[^a-zA-Z\s\-']/g, ''); // Fix spaces in name
                    }
                }
            }

            if (val !== originalVal) {
                this.value = val;
            }
        });
    }

    // Apply to existing inputs
    document.querySelectorAll('input, textarea').forEach(applySecurityToElement);

    // Mutation observer for dynamically added inputs (like admin modals)
    const observer = new MutationObserver((mutations) => {
        mutations.forEach(mutation => {
            mutation.addedNodes.forEach(node => {
                if (node.nodeType === 1) {
                    if (node.tagName === 'INPUT' || node.tagName === 'TEXTAREA') {
                        applySecurityToElement(node);
                    }
                    node.querySelectorAll('input, textarea').forEach(applySecurityToElement);
                }
            });
        });
    });
    observer.observe(document.body, { childList: true, subtree: true });
});
