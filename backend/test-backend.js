const express = require('express');
const app = express();
const PORT = 5001;

app.get('/', (req, res) => {
  res.send('Backend test: OK');
});

app.listen(PORT, () => {
  console.log(`Test server running on http://localhost:${PORT}`);
});

console.log('Express:', !!express);
try {
  require('mongoose');
  console.log('Mongoose: OK');
} catch (e) {
  console.log('Mongoose: Error -', e.message);
}
try {
  require('bcryptjs');
  console.log('bcryptjs: OK');
} catch (e) {
  console.log('bcryptjs: Error -', e.message);
}
try {
  require('jsonwebtoken');
  console.log('jsonwebtoken: OK');
} catch (e) {
  console.log('jsonwebtoken: Error -', e.message);
}
try {
  require('cors');
  console.log('cors: OK');
} catch (e) {
  console.log('cors: Error -', e.message);
}
try {
  require('dotenv');
  console.log('dotenv: OK');
} catch (e) {
  console.log('dotenv: Error -', e.message);
}
