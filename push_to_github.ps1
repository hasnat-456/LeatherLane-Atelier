$ErrorActionPreference = "Continue"

Set-Content -Path .gitignore -Value "back/bin/`nback/obj/`n*.db`n*.sqlite`nfront/uploads/`nfront/temp_backup/`n*.zip`n"

git init
git add .
git commit -m "Local full backup"
git branch backup-local

git remote add origin https://github.com/hasnat-456/LeatherLane-Atelier.git
git fetch origin

$coreFiles = @(
    ".gitignore",
    "back/Program.cs", 
    "back/LeatherLane.csproj", 
    "front/css/global.css", 
    "front/js/app.js",
    "back/app.db",
    "front/images",
    "package.json",
    "package-lock.json"
)

function CheckoutFiles {
    param([string[]]$Files)
    foreach ($file in $Files) {
        git checkout backup-local -- $file 2>$null
    }
}

# 1. Authentication
git checkout -B authentication origin/main
CheckoutFiles $coreFiles
CheckoutFiles @("front/login.html", "front/signup.html", "front/forgot-password.html", "front/reset-password.html", "front/js/auth.js", "back/Controllers/AuthController.cs", "back/Models/User.cs", "back/Models/LoginRequest.cs", "back/Models/RegisterRequest.cs", "back/Models/ResetPasswordRequest.cs")
git add .
git commit -m "Add authentication features"
git push -u origin authentication -f

# 2. Product Management
git checkout -B product-management origin/main
CheckoutFiles $coreFiles
CheckoutFiles @("front/products.html", "front/product-detail.html", "front/admin.html", "front/js/admin.js", "back/Controllers/ProductsController.cs", "back/Models/Product.cs", "back/Models/Category.cs", "front/uploads")
git add .
git commit -m "Add product management features"
git push -u origin product-management -f

# 3. Payments
git checkout -B payments origin/main
CheckoutFiles $coreFiles
CheckoutFiles @("front/checkout.html", "front/transactions.html", "front/cart.html", "back/Controllers/TransactionsController.cs", "back/Models/Transaction.cs", "back/Models/TransactionItem.cs", "back/Models/ManualPaymentSettings.cs", "back/Controllers/CartController.cs", "back/Models/CartItem.cs")
git add .
git commit -m "Add payment processing and manual payments"
git push -u origin payments -f

# 4. Notifications
git checkout -B notifications origin/main
CheckoutFiles $coreFiles
CheckoutFiles @("front/notifications.html", "front/admin-notifications.html", "back/Controllers/NotificationsController.cs", "back/Models/Notification.cs")
git add .
git commit -m "Add notifications system"
git push -u origin notifications -f

# 5. Header Footer Other Pages
git checkout -B header-footer-other-pages origin/main
CheckoutFiles $coreFiles
CheckoutFiles @("front/home.html", "front/about.html", "front/contact.html", "front/faq.html", "front/privacy-policy.html", "front/terms-and-conditions.html", "front/return-refund-policy.html", "front/shipping-policy.html", "front/exchange-tracking.html", "front/order-tracking.html", "front/return-tracking.html", "front/replacement-request.html", "front/index.html", "front/admin-exchanges.html")
git add .
git commit -m "Update layout, header, footer and static pages"
git push -u origin header-footer-other-pages -f

# Restore to local backup so the workspace remains intact for the user
git checkout backup-local
