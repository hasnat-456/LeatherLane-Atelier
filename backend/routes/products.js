const express = require('express');
const Product = require('../models/Product');
const Review = require('../models/Review');

const router = express.Router();

// Get all products with filters
router.get('/', async (req, res) => {
  try {
    const { category, subcategory, minPrice, maxPrice, sort, search, leatherType, color, gender, available, isNew, isBestseller, discount } = req.query;
    
    let query = {};

    if (category) query.category = category;
    if (subcategory) query.subcategory = subcategory;
    if (search) {
      query.$or = [
        { name: { $regex: search, $options: 'i' } },
        { description: { $regex: search, $options: 'i' } }
      ];
    }
    if (minPrice) query.price = { $gte: Number(minPrice) };
    if (maxPrice) query.price = { ...query.price, $lte: Number(maxPrice) };
    if (leatherType) query.leatherType = leatherType;
    if (color) query.colors = { $in: [color] };
    if (gender) query.gender = gender;
    if (available !== undefined) query.available = available === 'true';
    if (isNew) query.isNew = isNew === 'true';
    if (isBestseller) query.isBestseller = isBestseller === 'true';
    if (discount) query.discount = { $gte: Number(discount) };

    let sortOption = {};
    switch (sort) {
      case 'price-asc': sortOption = { price: 1 }; break;
      case 'price-desc': sortOption = { price: -1 }; break;
      case 'newest': sortOption = { createdAt: -1 }; break;
      case 'rating': sortOption = { rating: -1 }; break;
      case 'name': sortOption = { name: 1 }; break;
      default: sortOption = { createdAt: -1 }; break;
    }

    const products = await Product.find(query).sort(sortOption);
    res.json(products);
  } catch (err) {
    res.status(500).json({ message: 'Server error' });
  }
});

// Get single product
router.get('/:id', async (req, res) => {
  try {
    const product = await Product.findById(req.params.id).populate('relatedProducts').populate('frequentlyBoughtTogether');
    if (!product) return res.status(404).json({ message: 'Product not found' });
    res.json(product);
  } catch (err) {
    res.status(500).json({ message: 'Server error' });
  }
});

// Get product reviews
router.get('/:id/reviews', async (req, res) => {
  try {
    const reviews = await Review.find({ product: req.params.id }).populate('user', 'name');
    res.json(reviews);
  } catch (err) {
    res.status(500).json({ message: 'Server error' });
  }
});

// Create product (admin only, for now)
router.post('/', async (req, res) => {
  try {
    const product = new Product(req.body);
    await product.save();
    res.status(201).json(product);
  } catch (err) {
    res.status(400).json({ message: err.message });
  }
});

module.exports = router;
