import React from 'react';

const About = () => {
  return (
    <div style={styles.container}>
      {/* Hero Section */}
      <section style={styles.hero}>
        <div style={styles.heroContent}>
          <h1 style={styles.heroTitle}>Our Story</h1>
          <p style={styles.heroSubtitle}>Crafting Excellence Since 1985</p>
        </div>
      </section>

      {/* Story Section */}
      <section style={styles.story}>
        <div style={styles.storyGrid}>
          <div style={styles.storyContent}>
            <h2 style={styles.storyTitle}>The Art of Leather Crafting</h2>
            <p style={styles.storyText}>
              LeatherLane Atelier was founded with a simple yet profound belief: that true luxury lies in the marriage of timeless design and meticulous craftsmanship. For over three decades, we have dedicated ourselves to the art of creating leather goods that not only stand the test of time but become more beautiful with age.
            </p>
            <p style={styles.storyText}>
              Every piece in our collection is handcrafted by skilled artisans who bring decades of experience to their work. We source only the finest full-grain leather from sustainable tanneries around the world, ensuring both quality and ethical responsibility.
            </p>
            <p style={styles.storyText}>
              From our classic wallets to our signature bags, each item tells a story of passion, precision, and pride. We don't just make products—we create heirlooms.
            </p>
          </div>
          <div style={styles.storyImage}>
            <img 
              src="https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=artisan%20handcrafting%20leather%20in%20workshop%2C%20warm%20lighting%2C%20professional%20photography&image_size=square_hd" 
              alt="Artisan at Work" 
              style={styles.image}
            />
          </div>
        </div>
      </section>

      {/* Values Section */}
      <section style={styles.values}>
        <h2 style={styles.valuesTitle}>Our Values</h2>
        <div style={styles.valuesGrid}>
          <div style={styles.valueCard}>
            <div style={styles.valueIcon}>🌱</div>
            <h3 style={styles.valueTitle}>Sustainability</h3>
            <p style={styles.valueDesc}>We are committed to environmentally responsible practices, from sourcing to production.</p>
          </div>
          <div style={styles.valueCard}>
            <div style={styles.valueIcon}>⚒️</div>
            <h3 style={styles.valueTitle}>Craftsmanship</h3>
            <p style={styles.valueDesc}>Every piece is meticulously handcrafted by our skilled artisans.</p>
          </div>
          <div style={styles.valueCard}>
            <div style={styles.valueIcon}>💯</div>
            <h3 style={styles.valueTitle}>Quality</h3>
            <p style={styles.valueDesc}>We never compromise on quality. Only the finest materials make the cut.</p>
          </div>
          <div style={styles.valueCard}>
            <div style={styles.valueIcon}>🤝</div>
            <h3 style={styles.valueTitle}>Community</h3>
            <p style={styles.valueDesc}>We believe in supporting our artisans and local communities.</p>
          </div>
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
    background: 'linear-gradient(135deg, #8b5a2b 0%, #5c3d2e 100%)',
    padding: '5rem 2rem',
    textAlign: 'center',
    color: '#f5f0eb'
  },
  heroContent: {
    maxWidth: '800px',
    margin: '0 auto'
  },
  heroTitle: {
    fontSize: '3rem',
    marginBottom: '0.75rem',
    fontWeight: '700'
  },
  heroSubtitle: {
    fontSize: '1.3rem',
    opacity: '0.9'
  },
  story: {
    padding: '5rem 2rem',
    backgroundColor: '#ffffff'
  },
  storyGrid: {
    maxWidth: '1200px',
    margin: '0 auto',
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: '4rem',
    alignItems: 'center'
  },
  storyContent: {
    paddingRight: '2rem'
  },
  storyTitle: {
    fontSize: '2.2rem',
    color: '#3d2914',
    marginBottom: '1.5rem'
  },
  storyText: {
    color: '#5c4a3a',
    fontSize: '1.05rem',
    lineHeight: '1.8',
    marginBottom: '1.25rem'
  },
  storyImage: {
    borderRadius: '16px',
    overflow: 'hidden',
    boxShadow: '0 12px 40px rgba(92,61,46,0.15)'
  },
  image: {
    width: '100%',
    height: '450px',
    objectFit: 'cover'
  },
  values: {
    padding: '5rem 2rem',
    backgroundColor: '#f5f0eb'
  },
  valuesTitle: {
    fontSize: '2.5rem',
    color: '#3d2914',
    textAlign: 'center',
    marginBottom: '3rem'
  },
  valuesGrid: {
    maxWidth: '1200px',
    margin: '0 auto',
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))',
    gap: '2rem'
  },
  valueCard: {
    backgroundColor: '#ffffff',
    padding: '2.5rem',
    borderRadius: '16px',
    textAlign: 'center',
    boxShadow: '0 4px 16px rgba(92,61,46,0.08)',
    transition: 'all 0.3s ease',
    '&:hover': {
      transform: 'translateY(-10px)',
      boxShadow: '0 12px 40px rgba(92,61,46,0.15)'
    }
  },
  valueIcon: {
    fontSize: '3rem',
    marginBottom: '1rem'
  },
  valueTitle: {
    fontSize: '1.4rem',
    color: '#3d2914',
    marginBottom: '0.75rem'
  },
  valueDesc: {
    color: '#7a6b5d',
    lineHeight: '1.6'
  }
};

export default About;
