const path = require('path');

module.exports = {
  entry: './build/spv-channels.js',
  output: {
    filename: 'spv-channels.js',
    path: path.resolve(__dirname, 'dist'),
    libraryTarget: 'var',
    library: 'SPVChannels'
  },
	devtool: '',  
  externals: ['tls', 'net', 'fs']
};