import React, { useState, useEffect, useContext } from 'react';
import { AuthContext } from '../context/AuthContext';
import api from '../api/axios';

const Transactions = () => {
  const [transactions, setTransactions] = useState([]);
  const [loading, setLoading] = useState(true);
  const { user } = useContext(AuthContext);

  useEffect(() => {
    const fetchTransactions = async () => {
      if (!user) return;
      try {
        const { data } = await api.get('/transactions');
        setTransactions(data);
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    };
    fetchTransactions();
  }, [user]);

  const getPaymentMethodDisplay = (method) => {
    switch (method) {
      case 'jazzcash':
        return 'JazzCash';
      case 'easypaisa':
        return 'EasyPaisa';
      case 'card':
        return 'Credit/Debit Card';
      default:
        return method;
    }
  };

  const getPaymentDetailsDisplay = (details) => {
    if (!details) return '';
    if (details.phoneNumber) {
      return `Phone: ${details.phoneNumber}`;
    }
    if (details.cardLast4) {
      return `Card ending in: ${details.cardLast4}`;
    }
    return '';
  };

  if (loading) {
    return (
      <div style={styles.container}>
        <p style={styles.loading}>Loading transactions...</p>
      </div>
    );
  }

  return (
    <div style={styles.container}>
      <h2 style={styles.title}>Transaction History</h2>
      {transactions.length === 0 ? (
        <p style={styles.empty}>No transactions yet</p>
      ) : (
        <div style={styles.transactionList}>
          {transactions.map((tx) => (
            <div key={tx._id} style={styles.transactionCard}>
              <div style={styles.txHeader}>
                <span style={styles.txDate}>
                  {new Date(tx.createdAt).toLocaleDateString()}
                </span>
                <span style={{ ...styles.txStatus, ...(tx.status === 'completed' ? styles.statusCompleted : styles.statusPending) }}>
                  {tx.status}
                </span>
              </div>
              <div style={styles.txPayment}>
                <span style={styles.paymentMethodLabel}>
                  Payment: {getPaymentMethodDisplay(tx.paymentMethod)}
                </span>
                {tx.paymentDetails && (
                  <span style={styles.paymentDetails}>
                    {getPaymentDetailsDisplay(tx.paymentDetails)}
                  </span>
                )}
              </div>
              <div style={styles.txItems}>
                {tx.items.map((item, idx) => (
                  <div key={idx} style={styles.item}>
                    <span>{item.name} x {item.quantity}</span>
                    <span>${item.price * item.quantity}</span>
                  </div>
                ))}
              </div>
              <div style={styles.txFooter}>
                <span style={styles.totalLabel}>Total:</span>
                <span style={styles.totalAmount}>${tx.totalAmount}</span>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

const styles = {
  container: {
    maxWidth: '800px',
    margin: '0 auto',
    padding: '2rem',
  },
  title: {
    fontSize: '2rem',
    marginBottom: '2rem',
    color: '#3d2914',
  },
  loading: {
    textAlign: 'center',
    fontSize: '1.2rem',
    color: '#666',
    marginTop: '3rem',
  },
  empty: {
    textAlign: 'center',
    color: '#666',
    fontSize: '1.1rem',
    marginTop: '3rem',
  },
  transactionList: {
    display: 'flex',
    flexDirection: 'column',
    gap: '1.5rem',
  },
  transactionCard: {
    backgroundColor: '#fff',
    padding: '1.5rem',
    borderRadius: '8px',
    boxShadow: '0 2px 10px rgba(0,0,0,0.1)',
  },
  txHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    marginBottom: '1rem',
    paddingBottom: '1rem',
    borderBottom: '1px solid #eee',
  },
  txDate: {
    color: '#666',
  },
  txStatus: {
    padding: '0.3rem 0.8rem',
    borderRadius: '4px',
    fontSize: '0.9rem',
    fontWeight: 'bold',
  },
  statusCompleted: {
    backgroundColor: '#d5f5e3',
    color: '#27ae60',
  },
  statusPending: {
    backgroundColor: '#fef9e7',
    color: '#f39c12',
  },
  txPayment: {
    display: 'flex',
    flexDirection: 'column',
    gap: '0.5rem',
    marginBottom: '1rem',
    padding: '0.75rem',
    backgroundColor: '#f9f6f3',
    borderRadius: '4px',
  },
  paymentMethodLabel: {
    fontWeight: 'bold',
    color: '#5c3d2e',
  },
  paymentDetails: {
    color: '#7a6b5d',
    fontSize: '0.9rem',
  },
  txItems: {
    marginBottom: '1rem',
  },
  item: {
    display: 'flex',
    justifyContent: 'space-between',
    padding: '0.5rem 0',
    color: '#555',
  },
  txFooter: {
    display: 'flex',
    justifyContent: 'space-between',
    paddingTop: '1rem',
    borderTop: '1px solid #eee',
    fontSize: '1.2rem',
    fontWeight: 'bold',
    color: '#3d2914',
  },
};

export default Transactions;
