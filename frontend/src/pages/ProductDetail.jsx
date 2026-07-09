import React, { useContext, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { CartContext } from '../context/CartContext';

const ProductDetail = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const { products, addToCart } = useContext(CartContext);
  const [quantity, setQuantity] = useState(1);

  const product = products.find(p => p.id === parseInt(id));

  if (!product) {
    return (
      <div style={styles.notFound}>
        <h2>Product Not Found</h2>
        <button onClick={() => navigate('/')} style={styles.button}>
          Back to Home
        </button>
      </div>
    );
  }

  return (
    <div style={styles.container}>
      <div style={styles.productContainer}>
        <button onClick={() => navigate('/')} style={styles.backButton}>
          ← Back to Collection
        </button>
        <div style={styles.productGrid}>
          <div style={styles.imageContainer}>
            <img src={product.image} alt={product.name} style={styles.productImage} />
          </div>
          <div style={styles.productInfo}>
            <span style={styles.category}>{product.category}</span>
            <h1 style={styles.title}>{product.name}</h1>
            <div style={styles.priceContainer}>
              <span style={styles.price}>${product.price}</span>
              <span style={styles.stock}>In Stock: {product.stock}</span>
            </div>
            <p style={styles.description}>{product.description}</p>
            <div style={styles.quantitySection}>
              <label style={styles.quantityLabel}>Quantity</label>
              <div style={styles.quantityControls}>
                <button onClick={() => setQuantity(Math.max(1, quantity - 1))} style={styles.quantityButton}>-</button>
                <span style={styles.quantity}>{quantity}</span>
                <button onClick={() => setQuantity(Math.min(product.stock, quantity + 1))} style={styles.quantityButton}>+</button>
              </div>
            </div>
            <button onClick={() => addToCart(product, quantity)} style={styles.addButton}>
              Add to Cart
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

const styles = {
  container: {
    padding: '3rem 2rem',
    backgroundColor: '#f5f0eb',
    minHeight: '80vh'
  },
  productContainer: {
    maxWidth: '1100px',
    margin: '0 auto'
  },
  backButton: {
    backgroundColor: 'transparent',
    border: 'none',
    color: '#5c3d2e',
    fontSize: '1rem',
    fontWeight: '500',
    cursor: 'pointer',
    marginBottom: '2rem',
    padding: '0.5rem 0',
    transition: 'all 0.3s ease',
    '&:hover': {
      color: '#d4af37'
    }
  },
  productGrid: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: '4rem',
    backgroundColor: '#ffffff',
    padding: '3rem',
    borderRadius: '16px',
    boxShadow: '0 8px 32px rgba(92,61,46,0.1)'
  },
  imageContainer: {
    borderRadius: '12px',
    overflow: 'hidden'
  },
  productImage: {
    width: '100%',
    height: '500px',
    objectFit: 'cover',
    borderRadius: '12px'
  },
  productInfo: {
    display: 'flex',
    flexDirection: 'column',
    justifyContent: 'center',
    gap: '1.5rem'
  },
  category: {
    fontSize: '0.9rem',
    color: '#d4af37',
    fontWeight: '600',
    textTransform: 'uppercase',
    letterSpacing: '2px'
  },
  title: {
    fontSize: '2.5rem',
    color: '#3d2914',
    margin: '0.5rem 0'
  },
  priceContainer: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingBottom: '1.5rem',
    borderBottom: '1px solid #e0d9d3'
  },
  price: {
    fontSize: '2.2rem',
    fontWeight: '700',
    color: '#5c3d2e'
  },
  stock: {
    fontSize: '1rem',
    color: '#7a6b5d'
  },
  description: {
    fontSize: '1.1rem',
    color: '#5c4a3a',
    lineHeight: '1.8',
    paddingBottom: '1rem'
  },
  quantitySection: {
    display: 'flex',
    flexDirection: 'column',
    gap: '0.75rem'
  },
  quantityLabel: {
    fontSize: '1rem',
    color: '#5c3d2e',
    fontWeight: '600'
  },
  quantityControls: {
    display: 'flex',
    alignItems: 'center',
    gap: '1rem'
  },
  quantityButton: {
    width: '40px',
    height: '40px',
    border: '2px solid #5c3d2e',
    backgroundColor: '#ffffff',
    color: '#5c3d2e',
    borderRadius: '8px',
    fontSize: '1.25rem',
    fontWeight: '600',
    cursor: 'pointer',
    transition: 'all 0.3s ease',
    '&:hover': {
      backgroundColor: '#5c3d2e',
      color: '#f5f0eb'
    }
  },
  quantity: {
    fontSize: '1.3rem',
    fontWeight: '600',
    color: '#3d2914',
    minWidth: '30px',
    textAlign: 'center'
  },
  addButton: {
    backgroundColor: '#5c3d2e',
    color: '#f5f0eb',
    padding: '1rem 2rem',
    borderRadius: '8px',
    border: 'none',
    fontSize: '1.1rem',
    fontWeight: '600',
    cursor: 'pointer',
    transition: 'all 0.3s ease',
    marginTop: '1rem',
    '&:hover': {
      backgroundColor: '#8b5a2b',
      transform: 'translateY(-2px)',
      boxShadow: '0 8px 20px rgba(92,61,46,0.25)'
    }
  },
  notFound: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: '70vh',
    gap: '2rem',
    backgroundColor: '#f5f0eb'
  },
  button: {
    backgroundColor: '#5c3d2e',
    color: '#f5f0eb',
    padding: '1rem 2rem',
    borderRadius: '8px',
    border: 'none',
    fontSize: '1rem',
    fontWeight: '600',
    cursor: 'pointer',
    transition: 'all 0.3s ease'
  }
};

export default ProductDetail;
