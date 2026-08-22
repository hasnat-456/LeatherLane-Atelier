// security.js - Universal Input Validation & Anti-XSS
document.addEventListener('DOMContentLoaded', () => {
    function applySecurityToElement(input) {
        if (input.type === 'hidden' || input.dataset.secured) return;
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
            const isUrl = input.type === 'url' || name.includes('url') || id.includes('url') || name.includes('link') || id.includes('link');

            if (!isPassword) {
                if (isEmail) {
                    // Emails shouldn't have weird brackets or spaces
                    val = val.replace(/[^a-zA-Z0-9@\\.\\-_+]/g, '');
                } else if (isUrl) {
                    // Allow valid URL characters
                    val = val.replace(/[^a-zA-Z0-9\\-\\.\\_~:/?#\\[\\]@!$&'()*+,;=%]/g, '');
                } else {
                    // 2. Number fields (Phone, ZIP, Quantities, Prices)
                    const isPhoneOrMobile = name.includes('phone') || name.includes('mobile') || id.includes('phone') || id.includes('mobile');
                    const isZipOrPin = name.includes('zip') || (name.includes('pin') && !name.includes('shipping')) || id.includes('zip') || (id.includes('pin') && !id.includes('shipping'));
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
                        // 3. Name fields (Name, City, Category, etc.)
                        const isNameOrCity = (name.includes('name') && !name.includes('username')) || name.includes('city') || id.includes('name') || id.includes('city') || id.includes('category');
                        if (isNameOrCity) {
                            // Allow letters, spaces, hyphens, apostrophes
                            val = val.replace(/[^a-zA-Z\\s\\-']/g, '');
                        } else {
                            // 4. Addresses, Messages, Subjects, Descriptions
                            // Allow Alphanumeric, spaces, and normal punctuation (including # for apt numbers)
                            val = val.replace(/[^a-zA-Z0-9\\s\\-\\.,!?'"()&\\r\\n#:\\/=@%+_]/g, '');
                        }
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
