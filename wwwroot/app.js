const API_URL = 'http://localhost:5296/api';

let currentProduct = null;
let selectedSize = null;
let selectedColor = null;
let currentCategory = 'all';
let allProducts = [];
let filteredProducts = [];

// Filter state
let filters = {
    subcategories: [],
    brands: [],
    sizes: [],
    minPrice: 0,
    maxPrice: 10000
};

// Load products on page load
document.addEventListener('DOMContentLoaded', () => {
    loadProducts('all');
    updateCartCount();
    updateWishlistCount();
    
    // Category filter buttons
    document.querySelectorAll('.nav-btn').forEach(btn => {
        btn.addEventListener('click', function() {
            document.querySelectorAll('.nav-btn').forEach(b => b.classList.remove('active'));
            this.classList.add('active');
            const category = this.dataset.category;
            currentCategory = category;
            loadProducts(category);
        });
    });
});

// Format currency as Philippine Peso
function formatPeso(amount) {
    return `₱${amount.toLocaleString('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

// Load Products
async function loadProducts(category = 'all') {
    try {
        let url = `${API_URL}/products`;
        if (category === 'Men') url = `${API_URL}/products/men`;
        else if (category === 'Women') url = `${API_URL}/products/women`;
        
        const response = await fetch(url);
        const products = await response.json();
        allProducts = products;
        filteredProducts = products;
        
        updateBrandCounts();
        applyFilters();
    } catch (error) {
        console.error('Error loading products:', error);
    }
}

// Display Products
function displayProducts(products) {
    const grid = document.getElementById('products-grid');
    const productCount = document.getElementById('product-count');
    
    productCount.textContent = `(${products.length} product${products.length !== 1 ? 's' : ''})`;
    
    grid.innerHTML = products.map(product => `
        <div class="product-card" onclick="showProductDetails(${product.id})">
            <img src="${product.image}" alt="${product.name}" onerror="this.src='data:image/svg+xml,%3Csvg xmlns=%27http://www.w3.org/2000/svg%27 width=%27400%27 height=%27400%27%3E%3Crect width=%27400%27 height=%27400%27 fill=%27%23f5f5f5%27/%3E%3Ctext x=%2750%25%27 y=%2750%25%27 dominant-baseline=%27middle%27 text-anchor=%27middle%27 font-family=%27Inter%27 font-size=%2716%27 fill=%27%23a3a3a3%27%3EImage Not Available%3C/text%3E%3C/svg%3E'">
            <div class="product-card-content">
                <span class="category-badge">${product.category}</span>
                <h3>${product.name}</h3>
                <p class="description">${product.description}</p>
                <div class="price">${formatPeso(product.price)}</div>
                <div class="rating">
                    <span class="stars">${getStars(product.rating)}</span>
                    <span>${product.rating.toFixed(1)} (${product.reviewCount} reviews)</span>
                </div>
                <div class="product-actions" onclick="event.stopPropagation()">
                    <button class="btn-primary" onclick="quickAddToCart(${product.id})">Add to Cart</button>
                    <button class="btn-secondary" onclick="toggleWishlistProduct(${product.id})">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"></path>
                        </svg>
                    </button>
                </div>
            </div>
        </div>
    `).join('');
}

// Filter Functions
function applyFilters() {
    // Update filter state from checkboxes
    filters.subcategories = Array.from(document.querySelectorAll('input[name="subcategory"]:checked')).map(cb => cb.value);
    filters.brands = Array.from(document.querySelectorAll('input[name="brand"]:checked')).map(cb => cb.value);
    
    // Get price from inputs
    const minPriceInput = document.getElementById('min-price');
    const maxPriceInput = document.getElementById('max-price');
    if (minPriceInput.value) filters.minPrice = parseFloat(minPriceInput.value);
    if (maxPriceInput.value) filters.maxPrice = parseFloat(maxPriceInput.value);
    
    let filtered = [...allProducts];
    
    // Filter by subcategory
    if (filters.subcategories.length > 0) {
        filtered = filtered.filter(p => filters.subcategories.includes(p.subCategory));
    }
    
    // Filter by brand
    if (filters.brands.length > 0) {
        filtered = filtered.filter(p => filters.brands.includes(p.brand));
    }
    
    // Filter by size
    if (filters.sizes.length > 0) {
        filtered = filtered.filter(p => p.sizes.some(size => filters.sizes.includes(size)));
    }
    
    // Filter by price
    filtered = filtered.filter(p => p.price >= filters.minPrice && p.price <= filters.maxPrice);
    
    filteredProducts = filtered;
    displayProducts(filtered);
}

function clearAllFilters() {
    filters = {
        subcategories: [],
        brands: [],
        sizes: [],
        minPrice: 0,
        maxPrice: 10000
    };
    
    // Clear checkboxes
    document.querySelectorAll('input[type="checkbox"]').forEach(cb => cb.checked = false);
    
    // Clear size filters
    document.querySelectorAll('.size-filter-btn').forEach(btn => btn.classList.remove('active'));
   
    // Reset price inputs
    document.getElementById('min-price').value = '';
    document.getElementById('max-price').value = '';
    document.getElementById('price-slider').value = 10000;
    document.getElementById('price-display').textContent = '10,000';
    
    applyFilters();
}

function toggleSizeFilter(size) {
    const btn = event.target;
    btn.classList.toggle('active');
    
    if (btn.classList.contains('active')) {
        filters.sizes.push(size);
    } else {
        filters.sizes = filters.sizes.filter(s => s !== size);
    }
    
    applyFilters();
}

function updatePriceRange(maxValue) {
    filters.maxPrice = parseInt(maxValue);
    document.getElementById('price-display').textContent = formatPeso(filters.maxPrice).replace('₱', '').replace('.00', '');
    applyFilters();
}

function updateBrandCounts() {
    const nikeCount = allProducts.filter(p => p.brand === 'Nike').length;
    const adidasCount = allProducts.filter(p => p.brand === 'Adidas').length;
    
    const nikeCountEl = document.getElementById('nike-count');
    const adidasCountEl = document.getElementById('adidas-count');
    
    if (nikeCountEl) nikeCountEl.textContent = nikeCount;
    if (adidasCountEl) adidasCountEl.textContent = adidasCount;
}

function sortProducts(sortBy) {
    let sorted = [...filteredProducts];
    
    switch(sortBy) {
        case 'price-low':
            sorted.sort((a, b) => a.price - b.price);
            break;
        case 'price-high':
            sorted.sort((a, b) => b.price - a.price);
            break;
        case 'rating':
            sorted.sort((a, b) => b.rating - a.rating);
            break;
        case 'newest':
            sorted.sort((a, b) => b.id - a.id);
            break;
        default:
            // relevance - keep default order
            break;
    }
    
    displayProducts(sorted);
}

// Get related products
function getRelatedProducts(productId, category, limit = 4) {
    return allProducts
        .filter(p => p.id !== productId && p.category === category)
        .slice(0, limit);
}

// Show Product Details
async function showProductDetails(productId) {
    try {
        const response = await fetch(`${API_URL}/products/${productId}`);
        currentProduct = await response.json();
        
        document.getElementById('product-name').textContent = currentProduct.name;
        document.getElementById('product-image').src = currentProduct.image;
        document.getElementById('product-image').onerror = function() {
            this.src = "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='600' height='600'%3E%3Crect width='600' height='600' fill='%23f5f5f5'/%3E%3Ctext x='50%25' y='50%25' dominant-baseline='middle' text-anchor='middle' font-family='Inter' font-size='20' fill='%23a3a3a3'%3EImage Not Available%3C/text%3E%3C/svg%3E";
        };
        document.getElementById('product-description').textContent = currentProduct.description;
        document.getElementById('product-price').textContent = formatPeso(currentProduct.price);
        document.getElementById('product-rating').innerHTML = `
            <span class="stars">${getStars(currentProduct.rating)}</span>
            <span>${currentProduct.rating.toFixed(1)} (${currentProduct.reviewCount} reviews)</span>
        `;
        
        // Display sizes
        const sizeOptions = document.getElementById('size-options');
        sizeOptions.innerHTML = currentProduct.sizes.map(size => `
            <div class="size-option" onclick="selectSize('${size}')">${size}</div>
        `).join('');
        
        // Display colors
        const colorSection = document.getElementById('color-selector-section');
        if (currentProduct.availableColors && currentProduct.availableColors.length > 0) {
            colorSection.style.display = 'flex';
            const colorOptions = document.getElementById('color-options');
            colorOptions.innerHTML = currentProduct.availableColors.map(color => `
                <div class="color-option" onclick="selectColor('${color}')">${color}</div>
            `).join('');
        } else {
            colorSection.style.display = 'none';
        }
        
        // Display reviews preview (first 3)
        const reviewsPreview = document.getElementById('product-reviews-preview');
        if (currentProduct.reviews.length > 0) {
            const previewReviews = currentProduct.reviews.slice(0, 3);
            reviewsPreview.innerHTML = previewReviews.map(review => `
                <div class="review-item">
                    <div class="review-header">
                        <span class="review-name">${review.userName}</span>
                        <span class="review-date">${new Date(review.date).toLocaleDateString('en-PH')}</span>
                    </div>
                    <div class="rating">
                        <span class="stars">${getStars(review.rating)}</span>
                    </div>
                    <p>${review.comment}</p>
                </div>
            `).join('');
            
            // Show "View More" button if there are more than 3 reviews
            const viewMoreBtn = document.getElementById('view-more-reviews');
            if (currentProduct.reviews.length > 3) {
                viewMoreBtn.style.display = 'block';
                viewMoreBtn.textContent = `View All ${currentProduct.reviewCount} Reviews`;
            } else {
                viewMoreBtn.style.display = 'none';
            }
        } else {
            reviewsPreview.innerHTML = '<p style="color: var(--gray-500); padding: 24px 0;">No reviews yet. Be the first to share your experience!</p>';
            document.getElementById('view-more-reviews').style.display = 'none';
        }
        
        // Display related products in left column
        const relatedProducts = getRelatedProducts(currentProduct.id, currentProduct.category, 4);
        const relatedDiv = document.getElementById('related-products');
        if (relatedProducts.length > 0) {
            relatedDiv.innerHTML = relatedProducts.map(product => `
                <div class="related-product-card" onclick="showProductDetails(${product.id})">
                    <img src="${product.image}" alt="${product.name}" onerror="this.src='data:image/svg+xml,%3Csvg xmlns=%27http://www.w3.org/2000/svg%27 width=%27300%27 height=%27200%27%3E%3Crect width=%27300%27 height=%27200%27 fill=%27%23f5f5f5%27/%3E%3Ctext x=%2750%25%27 y=%2750%25%27 dominant-baseline=%27middle%27 text-anchor=%27middle%27 font-family=%27Inter%27 font-size=%2714%27 fill=%27%23a3a3a3%27%3ENo Image%3C/text%3E%3C/svg%3E'">
                    <div class="content">
                        <h4>${product.name}</h4>
                        <div class="price">${formatPeso(product.price)}</div>
                        <div class="rating">
                            <span class="stars">${getStars(product.rating)}</span>
                            <span style="font-size: 12px; color: var(--gray-600);">${product.rating.toFixed(1)}</span>
                        </div>
                    </div>
                </div>
            `).join('');
        } else {
            relatedDiv.innerHTML = '<p style="color: var(--gray-500);">No related products available.</p>';
        }
        
        document.getElementById('product-modal').classList.add('active');
        selectedSize = null;
        selectedColor = null;
    } catch (error) {
        console.error('Error loading product details:', error);
    }
}

function closeProductDetails() {
    document.getElementById('product-modal').classList.remove('active');
    currentProduct = null;
    selectedSize = null;
    selectedColor = null;
}

function selectSize(size) {
    selectedSize = size;
    document.querySelectorAll('.size-option').forEach(opt => opt.classList.remove('selected'));
    event.target.classList.add('selected');
}

function selectColor(color) {
    selectedColor = color;
    document.querySelectorAll('.color-option').forEach(opt => opt.classList.remove('selected'));
    event.target.classList.add('selected');
}

function buyNow() {
    if (!selectedSize) {
        alert('Please select a size');
        return;
    }
    
    // Add to cart then go to checkout
    addToCartFromModal();
    
    // Close product modal and open cart
    closeProductDetails();
    setTimeout(() => {
        toggleCart();
    }, 300);
}

// Open Reviews Modal
function openReviewsModal() {
    if (!currentProduct || !currentProduct.reviews || currentProduct.reviews.length === 0) {
        return;
    }
    
    const reviewsGrid = document.getElementById('all-reviews-grid');
    reviewsGrid.innerHTML = currentProduct.reviews.map(review => `
        <div class="review-item">
            <div class="review-header">
                <span class="review-name">${review.userName}</span>
                <span class="review-date">${new Date(review.date).toLocaleDateString('en-PH')}</span>
            </div>
            <div class="rating">
                <span class="stars">${getStars(review.rating)}</span>
            </div>
            <p>${review.comment}</p>
        </div>
    `).join('');
    
    document.getElementById('reviews-modal').classList.add('active');
}

function closeReviewsModal() {
    document.getElementById('reviews-modal').classList.remove('active');
}

// Add to Cart
async function quickAddToCart(productId) {
    const product = await fetch(`${API_URL}/products/${productId}`).then(r => r.json());
    const size = product.sizes[0]; // Default to first size
    
    await addToCart(productId, size);
}

async function addToCartFromModal() {
    if (!selectedSize) {
        alert('Please select a size');
        return;
    }
    
    await addToCart(currentProduct.id, selectedSize);
    closeProductDetails();
}

async function addToCart(productId, size) {
    try {
        const response = await fetch(`${API_URL}/cart`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ productId, size, quantity: 1 })
        });
        
        if (response.ok) {
            updateCartCount();
            alert('Added to cart!');
        }
    } catch (error) {
        console.error('Error adding to cart:', error);
    }
}

// Cart Functions
async function toggleCart() {
    const modal = document.getElementById('cart-modal');
    
    if (!modal.classList.contains('active')) {
        await loadCart();
    }
    
    modal.classList.toggle('active');
}

async function loadCart() {
    try {
        const response = await fetch(`${API_URL}/cart`);
        const cartItems = await response.json();
        
        const cartDiv = document.getElementById('cart-items');
        
        if (cartItems.length === 0) {
            cartDiv.innerHTML = '<p>Your cart is empty</p>';
            document.getElementById('cart-total').textContent = formatPeso(0);
            return;
        }
        
        cartDiv.innerHTML = cartItems.map(item => `
            <div class="cart-item">
                <img src="${item.product.image}" alt="${item.product.name}" onerror="this.src='data:image/svg+xml,%3Csvg xmlns=%27http://www.w3.org/2000/svg%27 width=%27120%27 height=%27120%27%3E%3Crect width=%27120%27 height=%27120%27 fill=%27%23f5f5f5%27/%3E%3C/svg%3E'">
                <div class="item-info">
                    <h4>${item.product.name}</h4>
                    <p>Size: ${item.cartItem.size}</p>
                    <p>Quantity: ${item.cartItem.quantity}</p>
                    <p class="price">${formatPeso(item.product.price)}</p>
                </div>
                <button class="remove-btn" onclick="removeFromCart(${item.product.id})">Remove</button>
            </div>
        `).join('');
        
        const total = cartItems.reduce((sum, item) => sum + (item.product.price * item.cartItem.quantity), 0);
        document.getElementById('cart-total').textContent = formatPeso(total);
    } catch (error) {
        console.error('Error loading cart:', error);
    }
}

async function removeFromCart(productId) {
    try {
        await fetch(`${API_URL}/cart/${productId}`, { method: 'DELETE' });
        await loadCart();
        updateCartCount();
    } catch (error) {
        console.error('Error removing from cart:', error);
    }
}

async function updateCartCount() {
    try {
        const response = await fetch(`${API_URL}/cart`);
        const cartItems = await response.json();
        document.getElementById('cart-count').textContent = cartItems.length;
    } catch (error) {
        console.error('Error updating cart count:', error);
    }
}

// Wishlist Functions
async function toggleWishlistProduct(productId) {
    await addToWishlist(productId);
}

async function toggleWishlistFromModal() {
    await addToWishlist(currentProduct.id);
}

async function addToWishlist(productId) {
    try {
        const response = await fetch(`${API_URL}/wishlist/${productId}`, { method: 'POST' });
        const result = await response.json();
        updateWishlistCount();
        alert(result.message);
    } catch (error) {
        console.error('Error adding to wishlist:', error);
    }
}

async function toggleWishlist() {
    const modal = document.getElementById('wishlist-modal');
    
    if (!modal.classList.contains('active')) {
        await loadWishlist();
    }
    
    modal.classList.toggle('active');
}

async function loadWishlist() {
    try {
        const response = await fetch(`${API_URL}/wishlist`);
        const wishlistItems = await response.json();
        
        const wishlistDiv = document.getElementById('wishlist-items');
        
        if (wishlistItems.length === 0) {
            wishlistDiv.innerHTML = '<p>Your wishlist is empty</p>';
            return;
        }
        
        wishlistDiv.innerHTML = wishlistItems.map(item => `
            <div class="wishlist-item">
                <img src="${item.product.image}" alt="${item.product.name}" onerror="this.src='data:image/svg+xml,%3Csvg xmlns=%27http://www.w3.org/2000/svg%27 width=%27120%27 height=%27120%27%3E%3Crect width=%27120%27 height=%27120%27 fill=%27%23f5f5f5%27/%3E%3C/svg%3E'">
                <div class="item-info">
                    <h4>${item.product.name}</h4>
                    <p class="price">${formatPeso(item.product.price)}</p>
                    <div class="rating">
                        <span class="stars">${getStars(item.product.rating)}</span>
                        <span>${item.product.rating.toFixed(1)}</span>
                    </div>
                </div>
                <button class="btn-primary" onclick="quickAddToCart(${item.product.id})">Add to Cart</button>
                <button class="remove-btn" onclick="removeFromWishlist(${item.product.id})">Remove</button>
            </div>
        `).join('');
    } catch (error) {
        console.error('Error loading wishlist:', error);
    }
}

async function removeFromWishlist(productId) {
    try {
        await fetch(`${API_URL}/wishlist/${productId}`, { method: 'DELETE' });
        await loadWishlist();
        updateWishlistCount();
    } catch (error) {
        console.error('Error removing from wishlist:', error);
    }
}

async function updateWishlistCount() {
    try {
        const response = await fetch(`${API_URL}/wishlist`);
        const wishlistItems = await response.json();
        document.getElementById('wishlist-count').textContent = wishlistItems.length;
    } catch (error) {
        console.error('Error updating wishlist count:', error);
    }
}

// Helper Functions
function getStars(rating) {
    const fullStars = Math.floor(rating);
    const halfStar = rating % 1 >= 0.5 ? 1 : 0;
    const emptyStars = 5 - fullStars - halfStar;
    
    return '★'.repeat(fullStars) + (halfStar ? '½' : '') + '☆'.repeat(emptyStars);
}
