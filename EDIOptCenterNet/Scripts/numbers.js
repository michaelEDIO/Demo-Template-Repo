// clean numbers
function CleanNum(t, v, n) {
	var NaN = false;
	// convert variable to string (for replace function)
	v = String(v);
	// remove commas
	v = v.replace(/,/g, '');
	// if number type requested
	if (n === 'integer' || n === 'float') {
		// create integer & clean white space
		if (n === 'integer') {
			v = parseInt(v, 10);
		}
		// create float & clean white space
		else {
			v = parseFloat(v).toFixed(2);
		}
	}
	// give same number type as entered
	else {
		// create integer & clean white space
		if (v.indexOf('.') == -1) {
			n = 'integer';
			v = parseInt(v, 10);
		}
		// create float & clean white space
		else {
			n = 'float';
			v = parseFloat(v).toFixed(2);
		}
	}
	// convert NaN to zero
	if (isNaN(v)) {
		NaN = true;
		if (n === 'integer') {
			v = 0;
		}
		else {
			v = parseFloat(0).toFixed(2);
		}
	}
	// if less than 0
	if (v < 0) {
		v = Math.abs(v);  // create absolute value
		if (n === 'float') {
			v = parseFloat(v).toFixed(2);
		}
	}
	// insert new value
	if (t !== '') {
		if (NaN) {
			t.val('');
		}
		else
			t.val(v);
	}
	// return new cleaned value
	return v;
}

// add commas
function addCommas(nStr) {
	nStr += '';
	x = nStr.split('.');
	x1 = x[0];
	x2 = x.length > 1 ? '.' + x[1] : '';
	var rgx = /(\d+)(\d{3})/;
	while (rgx.test(x1)) {
		x1 = x1.replace(rgx, '$1' + ',' + '$2');
	}
	return x1 + x2;
}

function removeCommas(nStr) {
	return nStr.replace(/,/g, ''); // String.replace normally only replaces first occurance
}
