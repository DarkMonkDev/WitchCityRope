const bcrypt = require('bcrypt');

const password = 'Test123!';
const hash = '$2a$11$cwwDaO2vKJ5YTwgysFDFtunSJ3hybw6iwvAsqeXGuQx6Mgc01wyYO';

bcrypt.compare(password, hash, function(err, result) {
    if (err) {
        console.error('Error:', err);
    } else {
        console.log('Password match:', result);
    }
});