import React, { createContext, useState, useEffect } from 'react';

export const CartContext = createContext();

const sampleProducts = [
  {
    id: 1,
    name: 'Classic Leather Wallet',
    price: 120,
    description: 'Handcrafted from full-grain leather, this classic wallet features multiple card slots and a bill compartment.',
    image: 'https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=classic%20handcrafted%20leather%20wallet%20on%20dark%20wooden%20background%2C%20professional%20product%20photography&image_size=square_hd',
    category: 'Wallets',
    stock: 50
  },
  {
    id: 2,
    name: 'Vintage Messenger Bag',
    price: 280,
    description: 'A timeless messenger bag made from premium leather, perfect for everyday use and travel.',
    image: 'https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=vintage%20leather%20messenger%20bag%2C%20professional%20product%20photography%2C%20brown%20leather&image_size=square_hd',
    category: 'Bags',
    stock: 25
  },
  {
    id: 3,
    name: 'Handcrafted Watch Strap',
    price: 65,
    description: 'Premium leather watch strap, available in multiple colors, hand-stitched for durability.',
    image: 'https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=handmade%20leather%20watch%20strap%20on%20wooden%20surface%2C%20product%20photography&image_size=square_hd',
    category: 'Accessories',
    stock: 100
  },
  {
    id: 4,
    name: 'Leather Briefcase',
    price: 450,
    description: 'Professional leather briefcase, perfect for business meetings and office use.',
    image: 'https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=premium%20leather%20briefcase%20for%20business%2C%20professional%20product%20photography&image_size=square_hd',
    category: 'Bags',
    stock: 15
  },
  {
    id: 5,
    name: 'Bifold Card Holder',
    price: 45,
    description: 'Slim leather card holder, perfect for carrying your essential cards in style.',
    image: 'https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=slim%20leather%20card%20holder%2C%20minimalist%20design%2C%20product%20photography&image_size=square_hd',
    category: 'Wallets',
    stock: 75
  },
  {
    id: 6,
    name: 'Leather Duffel Bag',
    price: 380,
    description: 'Spacious leather duffel bag, ideal for weekend getaways and travel.',
    image: 'https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=leather%20duffel%20bag%20for%20travel%2C%20handcrafted%2C%20product%20photography&image_size=square_hd',
    category: 'Bags',
    stock: 20
  }
];

export const CartProvider = ({ children }) => {
  const [cart, setCart] = useState([]);
  const [products] = useState(sampleProducts);

  useEffect(() => {
    const savedCart = localStorage.getItem('leatherlane_cart');
    if (savedCart) {
      setCart(JSON.parse(savedCart));
    }
  }, []);

  useEffect(() => {
    localStorage.setItem('leatherlane_cart', JSON.stringify(cart));
  }, [cart]);

  const addToCart = (product, quantity = 1) => {
    setCart(prev => {
      const existingItem = prev.find(item => item.id === product.id);
      if (existingItem) {
        return prev.map(item =>
          item.id === product.id ? { ...item, quantity: item.quantity + quantity } : item
        );
      }
      return [...prev, { ...product, quantity }];
    });
  };

  const removeFromCart = (productId) => {
    setCart(prev => prev.filter(item => item.id !== productId));
  };

  const updateQuantity = (productId, quantity) => {
    if (quantity <= 0) {
      removeFromCart(productId);
      return;
    }
    setCart(prev =>
      prev.map(item => item.id === productId ? { ...item, quantity } : item)
    );
  };

  const clearCart = () => {
    setCart([]);
  };

  const getCartTotal = () => {
    return cart.reduce((total, item) => total + item.price * item.quantity, 0);
  };

  const getCartCount = () => {
    return cart.reduce((count, item) => count + item.quantity, 0);
  };

  return (
    <CartContext.Provider
      value={{
        cart,
        products,
        addToCart,
        removeFromCart,
        updateQuantity,
        clearCart,
        getCartTotal,
        getCartCount
      }}
    >
      {children}
    </CartContext.Provider>
  );
};
