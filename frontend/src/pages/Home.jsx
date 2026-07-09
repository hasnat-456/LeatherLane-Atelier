import React, { useContext } from 'react';
import { Link } from 'react-router-dom';
import { CartContext } from '../context/CartContext';

const Home = () => {
  const { products, addToCart } = useContext(CartContext);

  return (
    <div style={styles.container}>
      {/* Hero Section */}
      <section style={styles.hero}>
        <div style={styles.heroContent}>
          <h1 style={styles.heroTitle}>Timeless Craftsmanship</h1>
          <p style={styles.heroSubtitle}>
            Handcrafted leather goods made with passion, precision, and the finest materials
          </p>
          <Link to="/#products" style={styles.heroButton}>
            Explore Collection
          </Link>
        </div>
      </section>

      {/* Features Section */}
      <section style={styles.features}>
        <div style={styles.featuresGrid}>
          <div style={styles.featureCard}>
            <div style={styles.featureIcon}>👜</div>
            <h3 style={styles.featureTitle}>Premium Quality</h3>
            <p style={styles.featureDesc}>Only the finest full-grain leather sourced sustainably</p>
          </div>
          <div style={styles.featureCard}>
            <div style={styles.featureIcon}>✂️</div>
            <h3 style={styles.featureTitle}>Handcrafted</h3>
            <p style={styles.featureDesc}>Each piece meticulously made by skilled artisans</p>
          </div>
          <div style={styles.featureCard}>
            <div style={styles.featureIcon}>🔒</div>
            <h3 style={styles.featureTitle}>Lifetime Warranty</h3>
            <p style={styles.featureDesc}>Quality that lasts generations</p>
          </div>
        </div>
      </section>

      {/* Products Section */}
      <section style={styles.products} id="products">
        <div style={styles.sectionHeader}>
          <h2 style={styles.sectionTitle}>Featured Products</h2>
          <p style={styles.sectionSubtitle}>Discover our handpicked collection</p>
        </div>
        <div style={styles.productGrid}>
          {products.map((product, idx) => (
            <div key={product.id} style={{ ...styles.productCard, animationDelay: `${idx * 0.1}s` }} className="fade-in">
              <div style={styles.productImageContainer}>
                <img src={product.image} alt={product.name} style={styles.productImage} />
                <div style={styles.productOverlay}>
                  <Link to={`/product/${product.id}`} style={styles.viewButton}>
                    View Details
                  </Link>
                </div>
              </div>
              <div style={styles.productInfo}>
                <span style={styles.productCategory}>{product.category}</span>
                <h3 style={styles.productName}>{product.name}</h3>
                <div style={styles.productBottom}>
                  <span style={styles.productPrice}>${product.price}</span>
                  <button 
                    onClick={() => addToCart(product)}
                    style={styles.addButton}
                  >
                    Add to Cart
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
};

const styles = {
  container: {
    width: '100%'
  },
  hero: {
    background: 'linear-gradient(135deg, #5c3d2e 0%, #8b5a2b 100%)',
    padding: '6rem 2rem',
    textAlign: 'center',
    color: '#f5f0eb',
    position: 'relative',
    overflow: 'hidden'
  },
  heroContent: {
    maxWidth: '800px',
    margin: '0 auto',
    position: 'relative',
    zIndex: 1
  },
  heroTitle: {
    fontSize: '3.5rem',
    marginBottom: '1rem',
    fontWeight: '700',
    letterSpacing: '2px'
  },
  heroSubtitle: {
    fontSize: '1.3rem',
    marginBottom: '2rem',
    opacity: '0.95',
    lineHeight: '1.8'
  },
  heroButton: {
    display: 'inline-block',
    backgroundColor: '#d4af37',
    color: '#3d2914',
    padding: '1rem 2.5rem',
    borderRadius: '8px',
    fontSize: '1.1rem',
    fontWeight: '600',
    boxShadow: '0 4px 16px rgba(212,175,55,0.3)',
    transition: 'all 0.3s ease',
    '&:hover': {
      backgroundColor: '#b8962e',
      transform: 'translateY(-3px)',
      boxShadow: '0 8px 24px rgba(212,175,55,0.4)'
    }
  },
  features: {
    padding: '4rem 2rem',
    backgroundColor: '#ffffff'
  },
  featuresGrid: {
    maxWidth: '1200px',
    margin: '0 auto',
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))',
    gap: '2.5rem'
  },
  featureCard: {
    textAlign: 'center',
    padding: '2rem',
    borderRadius: '12px',
    transition: 'all 0.3s ease',
    '&:hover': {
      transform: 'translateY(-8px)',
      boxShadow: '0 12px 40px rgba(92,61,46,0.12)'
    }
  },
  featureIcon: {
    fontSize: '3rem',
    marginBottom: '1rem'
  },
  featureTitle: {
    fontSize: '1.4rem',
    marginBottom: '0.75rem',
    color: '#3d2914'
  },
  featureDesc: {
    color: '#7a6b5d',
    fontSize: '1rem'
  },
  products: {
    padding: '5rem 2rem',
    backgroundColor: '#f5f0eb'
  },
  sectionHeader: {
    textAlign: 'center',
    marginBottom: '3rem'
  },
  sectionTitle: {
    fontSize: '2.5rem',
    marginBottom: '0.5rem',
    color: '#3d2914'
  },
  sectionSubtitle: {
    fontSize: '1.1rem',
    color: '#7a6b5d'
  },
  productGrid: {
    maxWidth: '1280px',
    margin: '0 auto',
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))',
    gap: '2.5rem'
  },
  productCard: {
    backgroundColor: '#ffffff',
    borderRadius: '16px',
    overflow: 'hidden',
    boxShadow: '0 4px 16px rgba(92,61,46,0.08)',
    transition: 'all 0.4s ease',
    '&:hover': {
      transform: 'translateY(-10px)',
      boxShadow: '0 16px 48px rgba(92,61,46,0.15)'
    }
  },
  productImageContainer: {
    position: 'relative',
    overflow: 'hidden'
  },
  productImage: {
    width: '100%',
    height: '320px',
    objectFit: 'cover',
    transition: 'transform 0.4s ease',
    '&:hover': {
      transform: 'scale(1.08)'
    }
  },
  productOverlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: 'rgba(92,61,46,0.7)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    opacity: 0,
    transition: 'all 0.3s ease',
    '&:hover': {
      opacity: 1
    }
  },
  viewButton: {
    backgroundColor: '#d4af37',
    color: '#3d2914',
    padding: '0.75rem 1.5rem',
    borderRadius: '8px',
    fontWeight: '600',
    transform: 'translateY(20px)',
    transition: 'all 0.3s ease',
    '&:hover': {
      backgroundColor: '#b8962e',
      transform: 'translateY(0)'
    }
  },
  productInfo: {
    padding: '1.5rem'
  },
  productCategory: {
    fontSize: '0.85rem',
    color: '#d4af37',
    fontWeight: '600',
    textTransform: 'uppercase',
    letterSpacing: '1px'
  },
  productName: {
    fontSize: '1.3rem',
    margin: '0.5rem 0',
    color: '#3d2914'
  },
  productBottom: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: '1rem'
  },
  productPrice: {
    fontSize: '1.4rem',
    fontWeight: '700',
    color: '#5c3d2e'
  },
  addButton: {
    backgroundColor: '#5c3d2e',
    color: '#f5f0eb',
    padding: '0.6rem 1.25rem',
    borderRadius: '6px',
    border: 'none',
    fontSize: '0.95rem',
    fontWeight: '600',
    transition: 'all 0.3s ease',
    '&:hover': {
      backgroundColor: '#8b5a2b',
      transform: 'scale(1.05)'
    }
  }
};

export default Home;
