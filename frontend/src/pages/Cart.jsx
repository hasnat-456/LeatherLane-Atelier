import React, { useContext } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { CartContext } from '../context/CartContext';

const Cart = () => {
  const { cart, removeFromCart, updateQuantity, getCartTotal, clearCart } = useContext(CartContext);
  const navigate = useNavigate();

  if (cart.length === 0) {
    return (
      <div style={styles.emptyCart}>
        <div style={styles.emptyCartContent}>
          <span style={styles.emptyIcon}>🛒</span>
          <h2 style={styles.emptyTitle}>Your Cart is Empty</h2>
          <p style={styles.emptyText}>Looks like you haven't added any items to your cart yet.</p>
          <Link to="/" style={styles.shopButton}>Continue Shopping</Link>
        </div>
      </div>
    );
  }

  return (
    <div style={styles.container}>
      <div style={styles.cartContainer}>
        <div style={styles.cartHeader}>
          <h1 style={styles.title}>Shopping Cart</h1>
          <button onClick={clearCart} style={styles.clearButton}>Clear Cart</button>
        </div>

        <div style={styles.cartGrid}>
          {/* Cart Items */}
          <div style={styles.cartItems}>
            {cart.map((item) => (
              <div key={item.id} style={styles.cartItem}>
                <img src={item.image} alt={item.name} style={styles.itemImage} />
                <div style={styles.itemDetails}>
                  <div style={styles.itemInfo}>
                    <h3 style={styles.itemName}>{item.name}</h3>
                    <p style={styles.itemCategory}>{item.category}</p>
                    <p style={styles.itemPrice}>${item.price}</p>
                  </div>
                  <div style={styles.itemActions}>
                    <div style={styles.quantityControls}>
                      <button onClick={() => updateQuantity(item.id, item.quantity - 1)} style={styles.quantityButton}>-</button>
                      <span style={styles.quantity}>{item.quantity}</span>
                      <button onClick={() => updateQuantity(item.id, item.quantity + 1)} style={styles.quantityButton}>+</button>
                    </div>
                    <div style={styles.itemTotal}>
                      <span style={styles.totalLabel}>Total:</span>
                      <span style={styles.totalPrice}>${(item.price * item.quantity).toFixed(2)}</span>
                    </div>
                    <button onClick={() => removeFromCart(item.id)} style={styles.removeButton}>Remove</button>
                  </div>
                </div>
              </div>
            ))}
          </div>

          {/* Order Summary */}
          <div style={styles.summaryContainer}>
            <div style={styles.summaryCard}>
              <h2 style={styles.summaryTitle}>Order Summary</h2>
              <div style={styles.summaryRow}>
                <span style={styles.summaryLabel}>Subtotal</span>
                <span style={styles.summaryValue}>${getCartTotal().toFixed(2)}</span>
              </div>
              <div style={styles.summaryRow}>
                <span style={styles.summaryLabel}>Shipping</span>
                <span style={styles.summaryValue}>Free</span>
              </div>
              <div style={styles.summaryRow}>
                <span style={styles.summaryLabel}>Tax</span>
                <span style={styles.summaryValue}>${(getCartTotal() * 0.08).toFixed(2)}</span>
              </div>
              <div style={styles.totalRow}>
                <span style={styles.totalLabel}>Total</span>
                <span style={styles.totalValue}>${(getCartTotal() * 1.08).toFixed(2)}</span>
              </div>
              <button onClick={() => navigate('/checkout')} style={styles.checkoutButton}>
                Proceed to Checkout
              </button>
              <Link to="/" style={styles.continueButton}>Continue Shopping</Link>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

const styles = {
  container: {
    backgroundColor: '#f5f0eb',
    minHeight: '85vh',
    padding: '3rem 2rem'
  },
  cartContainer: {
    maxWidth: '1200px',
    margin: '0 auto'
  },
  cartHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: '2rem'
  },
  title: {
    fontSize: '2.5rem',
    color: '#3d2914'
  },
  clearButton: {
    backgroundColor: 'transparent',
    color: '#c0392b',
    border: 'none',
    fontSize: '1rem',
    fontWeight: '600',
    cursor: 'pointer',
    padding: '0.5rem 1rem',
    transition: 'all 0.3s ease',
    '&:hover': {
      textDecoration: 'underline'
    }
  },
  cartGrid: {
    display: 'grid',
    gridTemplateColumns: '1.7fr 0.8fr',
    gap: '2.5rem'
  },
  cartItems: {
    display: 'flex',
    flexDirection: 'column',
    gap: '1.5rem'
  },
  cartItem: {
    backgroundColor: '#ffffff',
    padding: '1.5rem',
    borderRadius: '12px',
    display: 'flex',
    gap: '1.5rem',
    boxShadow: '0 4px 16px rgba(92,61,46,0.06)',
    transition: 'all 0.3s ease',
    '&:hover': {
      transform: 'translateY(-4px)',
      boxShadow: '0 8px 24px rgba(92,61,46,0.1)'
    }
  },
  itemImage: {
    width: '140px',
    height: '140px',
    objectFit: 'cover',
    borderRadius: '8px'
  },
  itemDetails: {
    flex: 1,
    display: 'flex',
    justifyContent: 'space-between'
  },
  itemInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: '0.5rem'
  },
  itemName: {
    fontSize: '1.4rem',
    color: '#3d2914',
    fontWeight: '600'
  },
  itemCategory: {
    fontSize: '0.95rem',
    color: '#7a6b5d'
  },
  itemPrice: {
    fontSize: '1.3rem',
    color: '#5c3d2e',
    fontWeight: '700'
  },
  itemActions: {
    display: 'flex',
    flexDirection: 'column',
    gap: '1rem',
    alignItems: 'flex-end',
    justifyContent: 'space-between'
  },
  quantityControls: {
    display: 'flex',
    alignItems: 'center',
    gap: '0.75rem'
  },
  quantityButton: {
    width: '36px',
    height: '36px',
    borderRadius: '6px',
    border: '2px solid #5c3d2e',
    backgroundColor: '#ffffff',
    color: '#5c3d2e',
    fontSize: '1.1rem',
    fontWeight: '600',
    cursor: 'pointer',
    transition: 'all 0.3s ease',
    '&:hover': {
      backgroundColor: '#5c3d2e',
      color: '#f5f0eb'
    }
  },
  quantity: {
    fontSize: '1.25rem',
    fontWeight: '600',
    color: '#3d2914',
    minWidth: '30px',
    textAlign: 'center'
  },
  itemTotal: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'flex-end',
    gap: '0.25rem'
  },
  totalLabel: {
    fontSize: '0.9rem',
    color: '#7a6b5d'
  },
  totalPrice: {
    fontSize: '1.35rem',
    color: '#3d2914',
    fontWeight: '700'
  },
  removeButton: {
    backgroundColor: 'transparent',
    color: '#c0392b',
    border: 'none',
    fontSize: '0.95rem',
    fontWeight: '600',
    cursor: 'pointer',
    padding: '0.5rem',
    transition: 'all 0.3s ease',
    '&:hover': {
      textDecoration: 'underline'
    }
  },
  summaryContainer: {
    height: 'fit-content',
    position: 'sticky',
    top: '120px'
  },
  summaryCard: {
    backgroundColor: '#ffffff',
    padding: '2rem',
    borderRadius: '12px',
    boxShadow: '0 4px 16px rgba(92,61,46,0.08)'
  },
  summaryTitle: {
    fontSize: '1.6rem',
    color: '#3d2914',
    marginBottom: '1.5rem',
    paddingBottom: '1rem',
    borderBottom: '1px solid #e0d9d3'
  },
  summaryRow: {
    display: 'flex',
    justifyContent: 'space-between',
    marginBottom: '1rem',
    fontSize: '1.05rem'
  },
  summaryLabel: {
    color: '#7a6b5d'
  },
  summaryValue: {
    color: '#5c4a3a',
    fontWeight: '600'
  },
  totalRow: {
    display: 'flex',
    justifyContent: 'space-between',
    marginBottom: '1.75rem',
    paddingTop: '1rem',
    borderTop: '1px solid #e0d9d3',
    fontSize: '1.4rem',
    fontWeight: '700'
  },
  totalLabel: {
    color: '#3d2914'
  },
  totalValue: {
    color: '#5c3d2e'
  },
  checkoutButton: {
    width: '100%',
    backgroundColor: '#5c3d2e',
    color: '#f5f0eb',
    padding: '1rem 1.5rem',
    borderRadius: '8px',
    border: 'none',
    fontSize: '1.1rem',
    fontWeight: '600',
    cursor: 'pointer',
    transition: 'all 0.3s ease',
    marginBottom: '1rem',
    '&:hover': {
      backgroundColor: '#8b5a2b',
      transform: 'translateY(-2px)',
      boxShadow: '0 6px 16px rgba(92,61,46,0.2)'
    }
  },
  continueButton: {
    display: 'block',
    width: '100%',
    textAlign: 'center',
    color: '#5c3d2e',
    fontWeight: '600',
    padding: '0.75rem',
    transition: 'all 0.3s ease',
    textDecoration: 'underline',
    '&:hover': {
      color: '#8b5a2b'
    }
  },
  emptyCart: {
    backgroundColor: '#f5f0eb',
    minHeight: '80vh',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    padding: '3rem 2rem'
  },
  emptyCartContent: {
    textAlign: 'center',
    backgroundColor: '#ffffff',
    padding: '4rem 3rem',
    borderRadius: '16px',
    boxShadow: '0 4px 16px rgba(92,61,46,0.08)'
  },
  emptyIcon: {
    fontSize: '4rem',
    marginBottom: '1rem',
    display: 'block'
  },
  emptyTitle: {
    fontSize: '2rem',
    color: '#3d2914',
    marginBottom: '0.75rem'
  },
  emptyText: {
    fontSize: '1.1rem',
    color: '#7a6b5d',
    marginBottom: '2rem'
  },
  shopButton: {
    display: 'inline-block',
    backgroundColor: '#5c3d2e',
    color: '#f5f0eb',
    padding: '1rem 2.5rem',
    borderRadius: '8px',
    fontWeight: '600',
    fontSize: '1.1rem',
    transition: 'all 0.3s ease',
    '&:hover': {
      backgroundColor: '#8b5a2b',
      transform: 'translateY(-2px)',
      boxShadow: '0 6px 16px rgba(92,61,46,0.2)'
    }
  }
};

export default Cart;
