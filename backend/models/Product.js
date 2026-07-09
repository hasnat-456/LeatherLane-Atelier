const mongoose = require('mongoose');

const productSchema = new mongoose.Schema({
  name: { type: String, required: true },
  slug: { type: String, required: true, unique: true },
  description: { type: String, required: true },
  price: { type: Number, required: true },
  originalPrice: { type: Number },
  category: { type: String, required: true },
  subcategory: { type: String },
  images: [{ type: String }],
  thumbnail: { type: String },
  leatherType: { type: String },
  colors: [{ type: String }],
  sizes: [{ type: String }],
  material: { type: String },
  gender: { type: String },
  stock: { type: Number, default: 0 },
  rating: { type: Number, default: 0 },
  numReviews: { type: Number, default: 0 },
  sku: { type: String },
  weight: { type: String },
  dimensions: { type: String },
  careInstructions: { type: String },
  isNew: { type: Boolean, default: false },
  isBestseller: { type: Boolean, default: false },
  discount: { type: Number },
  available: { type: Boolean, default: true },
  features: [{ type: String }],
  specifications: [
    {
      name: { type: String },
      value: { type: String }
    }
  ],
  relatedProducts: [{ type: mongoose.Schema.Types.ObjectId, ref: 'Product' }],
  frequentlyBoughtTogether: [{ type: mongoose.Schema.Types.ObjectId, ref: 'Product' }],
}, { timestamps: true });

module.exports = mongoose.model('Product', productSchema);
