/*
    http://www.JSON.org/json2.js
    2011-10-19

    Public Domain.

    NO WARRANTY EXPRESSED OR IMPLIED. USE AT YOUR OWN RISK.

    See http://www.JSON.org/js.html


    This code should be minified before deployment.
    See http://javascript.crockford.com/jsmin.html

    USE YOUR OWN COPY. IT IS EXTREMELY UNWISE TO LOAD CODE FROM SERVERS YOU DO
    NOT CONTROL.


    This file creates a global JSON object containing two methods: stringify
    and parse.

        JSON.stringify(value, replacer, space)
            value       any JavaScript value, usually an object or array.

            replacer    an optional parameter that determines how object
                        values are stringified for objects. It can be a
                        function or an array of strings.

            space       an optional parameter that specifies the indentation
                        of nested structures. If it is omitted, the text will
                        be packed without extra whitespace. If it is a number,
                        it will specify the number of spaces to indent at each
                        level. If it is a string (such as '\t' or '&nbsp;'),
                        it contains the characters used to indent at each level.

            This method produces a JSON text from a JavaScript value.

            When an object value is found, if the object contains a toJSON
            method, its toJSON method will be called and the result will be
            stringified. A toJSON method does not serialize: it returns the
            value represented by the name/value pair that should be serialized,
            or undefined if nothing should be serialized. The toJSON method
            will be passed the key associated with the value, and this will be
            bound to the value

            For example, this would serialize Dates as ISO strings.

                Date.prototype.toJSON = function (key) {
                    function f(n) {
                        // Format integers to have at least two digits.
                        return n < 10 ? '0' + n : n;
                    }

                    return this.getUTCFullYear()   + '-' +
                         f(this.getUTCMonth() + 1) + '-' +
                         f(this.getUTCDate())      + 'T' +
                         f(this.getUTCHours())     + ':' +
                         f(this.getUTCMinutes())   + ':' +
                         f(this.getUTCSeconds())   + 'Z';
                };

            You can provide an optional replacer method. It will be passed the
            key and value of each member, with this bound to the containing
            object. The value that is returned from your method will be
            serialized. If your method returns undefined, then the member will
            be excluded from the serialization.

            If the replacer parameter is an array of strings, then it will be
            used to select the members to be serialized. It filters the results
            such that only members with keys listed in the replacer array are
            stringified.

            Values that do not have JSON representations, such as undefined or
            functions, will not be serialized. Such values in objects will be
            dropped; in arrays they will be replaced with null. You can use
            a replacer function to replace those with JSON values.
            JSON.stringify(undefined) returns undefined.

            The optional space parameter produces a stringification of the
            value that is filled with line breaks and indentation to make it
            easier to read.

            If the space parameter is a non-empty string, then that string will
            be used for indentation. If the space parameter is a number, then
            the indentation will be that many spaces.

            Example:

            text = JSON.stringify(['e', {pluribus: 'unum'}]);
            // text is '["e",{"pluribus":"unum"}]'


            text = JSON.stringify(['e', {pluribus: 'unum'}], null, '\t');
            // text is '[\n\t"e",\n\t{\n\t\t"pluribus": "unum"\n\t}\n]'

            text = JSON.stringify([new Date()], function (key, value) {
                return this[key] instanceof Date ?
                    'Date(' + this[key] + ')' : value;
            });
            // text is '["Date(---current time---)"]'


        JSON.parse(text, reviver)
            This method parses a JSON text to produce an object or array.
            It can throw a SyntaxError exception.

            The optional reviver parameter is a function that can filter and
            transform the results. It receives each of the keys and values,
            and its return value is used instead of the original value.
            If it returns what it received, then the structure is not modified.
            If it returns undefined then the member is deleted.

            Example:

            // Parse the text. Values that look like ISO date strings will
            // be converted to Date objects.

            myData = JSON.parse(text, function (key, value) {
                var a;
                if (typeof value === 'string') {
                    a =
/^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2}(?:\.\d*)?)Z$/.exec(value);
                    if (a) {
                        return new Date(Date.UTC(+a[1], +a[2] - 1, +a[3], +a[4],
                            +a[5], +a[6]));
                    }
                }
                return value;
            });

            myData = JSON.parse('["Date(09/09/2001)"]', function (key, value) {
                var d;
                if (typeof value === 'string' &&
                        value.slice(0, 5) === 'Date(' &&
                        value.slice(-1) === ')') {
                    d = new Date(value.slice(5, -1));
                    if (d) {
                        return d;
                    }
                }
                return value;
            });


    This is a reference implementation. You are free to copy, modify, or
    redistribute.
*/

/*jslint evil: true, regexp: true */

/*members "", "\b", "\t", "\n", "\f", "\r", "\"", JSON, "\\", apply,
    call, charCodeAt, getUTCDate, getUTCFullYear, getUTCHours,
    getUTCMinutes, getUTCMonth, getUTCSeconds, hasOwnProperty, join,
    lastIndex, length, parse, prototype, push, replace, slice, stringify,
    test, toJSON, toString, valueOf
*/


// Create a JSON object only if one does not already exist. We create the
// methods in a closure to avoid creating global variables.
var JSONIDENTIFIER = JSONIDENTIFIER || {};
var JSON;
if (!JSON) {
    JSON = {};
}

(function () {
    'use strict';

    function f(n) {
        // Format integers to have at least two digits.
        return n < 10 ? '0' + n : n;
    }

    if (typeof Date.prototype.toJSON !== 'function') {

        Date.prototype.toJSON = function (key) {

            return isFinite(this.valueOf())
                ? this.getUTCFullYear()     + '-' +
                    f(this.getUTCMonth() + 1) + '-' +
                    f(this.getUTCDate())      + 'T' +
                    f(this.getUTCHours())     + ':' +
                    f(this.getUTCMinutes())   + ':' +
                    f(this.getUTCSeconds())   + 'Z'
                : null;
        };

        String.prototype.toJSON      =
            Number.prototype.toJSON  =
            Boolean.prototype.toJSON = function (key) {
                return this.valueOf();
            };
    }

    var cx = /[\u0000\u00ad\u0600-\u0604\u070f\u17b4\u17b5\u200c-\u200f\u2028-\u202f\u2060-\u206f\ufeff\ufff0-\uffff]/g,
        escapable = /[\\\"\x00-\x1f\x7f-\x9f\u00ad\u0600-\u0604\u070f\u17b4\u17b5\u200c-\u200f\u2028-\u202f\u2060-\u206f\ufeff\ufff0-\uffff]/g,
        gap,
        indent,
        meta = {    // table of character substitutions
            '\b': '\\b',
            '\t': '\\t',
            '\n': '\\n',
            '\f': '\\f',
            '\r': '\\r',
            '"' : '\\"',
            '\\': '\\\\'
        },
        rep;


    function quote(string) {

// If the string contains no control characters, no quote characters, and no
// backslash characters, then we can safely slap some quotes around it.
// Otherwise we must also replace the offending characters with safe escape
// sequences.

        escapable.lastIndex = 0;
        return escapable.test(string) ? '"' + string.replace(escapable, function (a) {
            var c = meta[a];
            return typeof c === 'string'
                ? c
                : '\\u' + ('0000' + a.charCodeAt(0).toString(16)).slice(-4);
        }) + '"' : '"' + string + '"';
    }


    function str(key, holder) {

// Produce a string from holder[key].

        var i,          // The loop counter.
            k,          // The member key.
            v,          // The member value.
            length,
            mind = gap,
            partial,
            value = holder[key];

// If the value has a toJSON method, call it to obtain a replacement value.

        if (value && typeof value === 'object' &&
                typeof value.toJSON === 'function') {
            value = value.toJSON(key);
        }

// If we were called with a replacer function, then call the replacer to
// obtain a replacement value.

        if (typeof rep === 'function') {
            value = rep.call(holder, key, value);
        }

// What happens next depends on the value's type.

        switch (typeof value) {
        case 'string':
            return quote(value);

        case 'number':

// JSON numbers must be finite. Encode non-finite numbers as null.

            return isFinite(value) ? String(value) : 'null';

        case 'boolean':
        case 'null':

// If the value is a boolean or null, convert it to a string. Note:
// typeof null does not produce 'null'. The case is included here in
// the remote chance that this gets fixed someday.

            return String(value);

// If the type is 'object', we might be dealing with an object or an array or
// null.

        case 'object':

// Due to a specification blunder in ECMAScript, typeof null is 'object',
// so watch out for that case.

            if (!value) {
                return 'null';
            }

// Make an array to hold the partial results of stringifying this object value.

            gap += indent;
            partial = [];

// Is the value an array?

            if (Object.prototype.toString.apply(value) === '[object Array]') {

// The value is an array. Stringify every element. Use null as a placeholder
// for non-JSON values.

                length = value.length;
                for (i = 0; i < length; i += 1) {
                    partial[i] = str(i, value) || 'null';
                }

// Join all of the elements together, separated with commas, and wrap them in
// brackets.

                v = partial.length === 0
                    ? '[]'
                    : gap
                    ? '[\n' + gap + partial.join(',\n' + gap) + '\n' + mind + ']'
                    : '[' + partial.join(',') + ']';
                gap = mind;
                return v;
            }

// If the replacer is an array, use it to select the members to be stringified.

            if (rep && typeof rep === 'object') {
                length = rep.length;
                for (i = 0; i < length; i += 1) {
                    if (typeof rep[i] === 'string') {
                        k = rep[i];
                        v = str(k, value);
                        if (v) {
                            partial.push(quote(k) + (gap ? ': ' : ':') + v);
                        }
                    }
                }
            } else {

// Otherwise, iterate through all of the keys in the object.

                for (k in value) {
                    if (Object.prototype.hasOwnProperty.call(value, k)) {
                        v = str(k, value);
                        if (v) {
                            partial.push(quote(k) + (gap ? ': ' : ':') + v);
                        }
                    }
                }
            }

// Join all of the member texts together, separated with commas,
// and wrap them in braces.

            v = partial.length === 0
                ? '{}'
                : gap
                ? '{\n' + gap + partial.join(',\n' + gap) + '\n' + mind + '}'
                : '{' + partial.join(',') + '}';
            gap = mind;
            return v;
        }
    }

// If the JSON object does not yet have a stringify method, give it one.

    if (typeof JSON.stringify !== 'function') {
        JSON.stringify = function (value, replacer, space) {

// The stringify method takes a value and an optional replacer, and an optional
// space parameter, and returns a JSON text. The replacer can be a function
// that can replace values, or an array of strings that will select the keys.
// A default replacer method can be provided. Use of the space parameter can
// produce text that is more easily readable.

            var i;
            gap = '';
            indent = '';

// If the space parameter is a number, make an indent string containing that
// many spaces.

            if (typeof space === 'number') {
                for (i = 0; i < space; i += 1) {
                    indent += ' ';
                }

// If the space parameter is a string, it will be used as the indent string.

            } else if (typeof space === 'string') {
                indent = space;
            }

// If there is a replacer, it must be a function or an array.
// Otherwise, throw an error.

            rep = replacer;
            if (replacer && typeof replacer !== 'function' &&
                    (typeof replacer !== 'object' ||
                    typeof replacer.length !== 'number')) {
                throw new Error('JSON.stringify');
            }

// Make a fake root object containing our value under the key of ''.
// Return the result of stringifying the value.

            return str('', {'': value});
        };
    }


// If the JSON object does not yet have a parse method, give it one.

    if (typeof JSON.parse !== 'function') {
        JSON.parse = function (text, reviver) {

// The parse method takes a text and an optional reviver function, and returns
// a JavaScript value if the text is a valid JSON text.

            var j;

            function walk(holder, key) {

// The walk method is used to recursively walk the resulting structure so
// that modifications can be made.

                var k, v, value = holder[key];
                if (value && typeof value === 'object') {
                    for (k in value) {
                        if (Object.prototype.hasOwnProperty.call(value, k)) {
                            v = walk(value, k);
                            if (v !== undefined) {
                                value[k] = v;
                            } else {
                                delete value[k];
                            }
                        }
                    }
                }
                return reviver.call(holder, key, value);
            }


// Parsing happens in four stages. In the first stage, we replace certain
// Unicode characters with escape sequences. JavaScript handles many characters
// incorrectly, either silently deleting them, or treating them as line endings.

            text = String(text);
            cx.lastIndex = 0;
            if (cx.test(text)) {
                text = text.replace(cx, function (a) {
                    return '\\u' +
                        ('0000' + a.charCodeAt(0).toString(16)).slice(-4);
                });
            }

// In the second stage, we run the text against regular expressions that look
// for non-JSON patterns. We are especially concerned with '()' and 'new'
// because they can cause invocation, and '=' because it can cause mutation.
// But just to be safe, we want to reject all unexpected forms.

// We split the second stage into 4 regexp operations in order to work around
// crippling inefficiencies in IE's and Safari's regexp engines. First we
// replace the JSON backslash pairs with '@' (a non-JSON character). Second, we
// replace all simple value tokens with ']' characters. Third, we delete all
// open brackets that follow a colon or comma or that begin the text. Finally,
// we look to see that the remaining characters are only whitespace or ']' or
// ',' or ':' or '{' or '}'. If that is so, then the text is safe for eval.

            if (/^[\],:{}\s]*$/
                    .test(text.replace(/\\(?:["\\\/bfnrt]|u[0-9a-fA-F]{4})/g, '@')
                        .replace(/"[^"\\\n\r]*"|true|false|null|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?/g, ']')
                        .replace(/(?:^|:|,)(?:\s*\[)+/g, ''))) {

// In the third stage we use the eval function to compile the text into a
// JavaScript structure. The '{' operator is subject to a syntactic ambiguity
// in JavaScript: it can begin a block or an object literal. We wrap the text
// in parens to eliminate the ambiguity.

                j = eval('(' + text + ')');

// In the optional fourth stage, we recursively walk the new structure, passing
// each name/value pair to a reviver function for possible transformation.

                return typeof reviver === 'function'
                    ? walk({'': j}, '')
                    : j;
            }

// If the text is not JSON parseable, then a SyntaxError is thrown.

            throw new SyntaxError('JSON.parse');
        };
    }
}());

/*
	Json Data Binding Library -- JavaScript
	Copyright Longflow Enterprises Ltd
	2011

	JavaScript library for providing data binding between HTML elements and data sources in web server.
	The web server may use PHP, .NET, CGI, and any other server programming technologies.
*/
var JsonDataBinding = JsonDataBinding || {
	NullDisplay: '', //'{null}' is causing confusion
	NullDisplayText: '{null}',
	DebugLevel: 0, //show debug information in a popup. 0: no debug.
	UseLocalStore: false, //default to use cookies, true: use HTML5 local storage
	print: function (e) {
		if (e && e.outerHTML) {
			var w = window.open('', '', 'left=0,top=0,width=800,height=900,toolbar=0,scrollbars=0,status=0');
			w.document.write(e.outerHTML);
			w.document.close();
			w.focus();
			w.print();
			w.close();
		}
	},
	toText: function(v) {
		if (typeof (v) == 'undefined') {
			return '';
		}
		if (v == null)
			return '';
		if (v instanceof Date) {
			return JsonDataBinding.datetime.toIso(v);
		}
		if (typeof (v.toTimeString) == 'function')
			return v.toTimeString();
		return v;
	},
	OpenDebugWindow: function() {
		return window.top.open("", "debugWindows");
	},
	ShowDebugInfoLine: function(msg) {
		if (JsonDataBinding.DebugLevel > 0) {
			var winDebug = JsonDataBinding.OpenDebugWindow(); //window.open("", "debugWindows");
			if (winDebug == null) {
				alert('Debug information cannot be displayed. Your web browser has disabled pop-up window');
			}
			else {
				winDebug.document.write(JsonDataBinding.datetime.toIso(new Date()));
				winDebug.document.write(' - ');
				winDebug.document.write(msg);
				winDebug.document.write('<br>');
			}
		}
	},
	fireEvent: function(sender, eventName) {
		if (!sender) {
			return;
		}
		try {
			var eventObj;
			if (document.createEvent) {
				eventObj = document.createEvent('HTMLEvents');
				//if (JsonDataBinding.IsIE()) {
				//if (!JsonDataBinding.startsWith(eventName, 'on')) {
				//	eventName = 'on' + eventName;
				//}
				//}
				//else {
				if (JsonDataBinding.startsWith(eventName, 'on')) {
					eventName = eventName.substr(2);
				}
				//}
				eventObj.initEvent(eventName, true, true);
				sender.dispatchEvent(eventObj);
			} else if (document.createEventObject) {
				eventObj = document.createEventObject();
				if (!JsonDataBinding.startsWith(eventName, 'on')) {
					eventName = 'on' + eventName;
				}
				sender.fireEvent(eventName, eventObj);
			} else {
				if (!JsonDataBinding.startsWith(eventName, 'on')) {
					eventName = 'on' + eventName;
				}
				if (sender[eventName]) {
					sender[eventName]();
				}
			}
		}
		catch (e) {
			alert(e.message ? e.message : e);
		}
	},
	//************************************************************************************
	// UTF-8 Encoding helpers.
	// based on the code at http://www.webtoolkit.info
	//************************************************************************************
	bytes2utf8: function() {
		function _encode(stringToEncode, insertBOM) {
			stringToEncode = stringToEncode.replace(/\r\n/g, "\n");
			var utftext = [];
			if (insertBOM == true) {
				utftext[0] = 0xef;
				utftext[1] = 0xbb;
				utftext[2] = 0xbf;
			}

			for (var n = 0; n < stringToEncode.length; n++) {

				var c = stringToEncode.charCodeAt(n);

				if (c < 128) {
					utftext[utftext.length] = c;
				}
				else if ((c > 127) && (c < 2048)) {
					utftext[utftext.length] = (c >> 6) | 192;
					utftext[utftext.length] = (c & 63) | 128;
				}
				else {
					utftext[utftext.length] = (c >> 12) | 224;
					utftext[utftext.length] = ((c >> 6) & 63) | 128;
					utftext[utftext.length] = (c & 63) | 128;
				}

			}
			return utftext;
		};

		var obj = {
			/**
			* Encode javascript string as utf8 byte array
			*/
			encode: function(stringToEncode) {
				return _encode(stringToEncode, false);
			},

			/**
			* Encode javascript string as utf8 byte array, with a BOM at the start
			*/
			encodeWithBOM: function(stringToEncode) {
				return _encode(stringToEncode, true);
			},

			/**
			* Decode utf8 byte array to javascript string....
			*/
			decode: function(dotNetBytes) {
				var result = "";
				var i = 0;
				var c = c1 = c2 = 0;

				// Perform byte-order check.
				if (dotNetBytes.length >= 3) {
					if ((dotNetBytes[0] & 0xef) == 0xef
						&& (dotNetBytes[1] & 0xbb) == 0xbb
						&& (dotNetBytes[2] & 0xbf) == 0xbf) {
						// Hmm byte stream has a BOM at the start, we'll skip this.
						i = 3;
					}
				}

				while (i < dotNetBytes.length) {
					c = dotNetBytes[i] & 0xff;

					if (c < 128) {
						result += String.fromCharCode(c);
						i++;
					}
					else if ((c > 191) && (c < 224)) {
						if (i + 1 >= dotNetBytes.length)
							throw "Un-expected encoding error, UTF-8 stream truncated, or incorrect";
						c2 = dotNetBytes[i + 1] & 0xff;
						result += String.fromCharCode(((c & 31) << 6) | (c2 & 63));
						i += 2;
					}
					else {
						if (i + 2 >= dotNetBytes.length || i + 1 >= dotNetBytes.length)
							throw "Un-expected encoding error, UTF-8 stream truncated, or incorrect";
						c2 = dotNetBytes[i + 1] & 0xff;
						c3 = dotNetBytes[i + 2] & 0xff;
						result += String.fromCharCode(((c & 15) << 12) | ((c2 & 63) << 6) | (c3 & 63));
						i += 3;
					}
				}
				return result;
			}
		};
		return obj;
	} (),
	Base64: function() {
		// private property
		var _keyStr = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";
		// public method for encoding
		_base64encode = function(input) {
			if (typeof input != 'undefined') {
				var output = "";
				var chr1, chr2, chr3, enc1, enc2, enc3, enc4;
				var i = 0;
				input = _utf8_encode(input);
				while (i < input.length) {
					chr1 = input.charCodeAt(i++);
					chr2 = input.charCodeAt(i++);
					chr3 = input.charCodeAt(i++);
					enc1 = chr1 >> 2;
					enc2 = ((chr1 & 3) << 4) | (chr2 >> 4);
					enc3 = ((chr2 & 15) << 2) | (chr3 >> 6);
					enc4 = chr3 & 63;
					if (isNaN(chr2)) {
						enc3 = enc4 = 64;
					} else if (isNaN(chr3)) {
						enc4 = 64;
					}
					output = output +
					_keyStr.charAt(enc1) + _keyStr.charAt(enc2) +
					_keyStr.charAt(enc3) + _keyStr.charAt(enc4);
				}
				return output;
			}
		}
		// public method for decoding
		_base64decode = function(input) {
			if (typeof input != 'undefined') {
				var output = "";
				var chr1, chr2, chr3;
				var enc1, enc2, enc3, enc4;
				var i = 0;
				input = input.replace(/[^A-Za-z0-9\+\/\=]/g, "");
				while (i < input.length) {
					enc1 = _keyStr.indexOf(input.charAt(i++));
					enc2 = _keyStr.indexOf(input.charAt(i++));
					enc3 = _keyStr.indexOf(input.charAt(i++));
					enc4 = _keyStr.indexOf(input.charAt(i++));
					chr1 = (enc1 << 2) | (enc2 >> 4);
					chr2 = ((enc2 & 15) << 4) | (enc3 >> 2);
					chr3 = ((enc3 & 3) << 6) | enc4;
					output = output + String.fromCharCode(chr1);
					if (enc3 != 64) {
						output = output + String.fromCharCode(chr2);
					}
					if (enc4 != 64) {
						output = output + String.fromCharCode(chr3);
					}
				}
				output = _utf8_decode(output);
				return output;
			}
		}
		// private method for UTF-8 encoding
		function _utf8_encode(string) {
			string = string.replace(/\r\n/g, "\n");
			var utftext = "";
			for (var n = 0; n < string.length; n++) {
				var c = string.charCodeAt(n);
				if (c < 128) {
					utftext += String.fromCharCode(c);
				}
				else if ((c > 127) && (c < 2048)) {
					utftext += String.fromCharCode((c >> 6) | 192);
					utftext += String.fromCharCode((c & 63) | 128);
				}
				else {
					utftext += String.fromCharCode((c >> 12) | 224);
					utftext += String.fromCharCode(((c >> 6) & 63) | 128);
					utftext += String.fromCharCode((c & 63) | 128);
				}
			}
			return utftext;
		}
		// private method for UTF-8 decoding
		function _utf8_decode(utftext) {
			var string = "";
			var i = 0;
			var c = 0, c1 = 0, c2 = 0;
			while (i < utftext.length) {
				c = utftext.charCodeAt(i);
				if (c < 128) {
					string += String.fromCharCode(c);
					i++;
				}
				else if ((c > 191) && (c < 224)) {
					c2 = utftext.charCodeAt(i + 1);
					string += String.fromCharCode(((c & 31) << 6) | (c2 & 63));
					i += 2;
				}
				else {
					c2 = utftext.charCodeAt(i + 1);
					c3 = utftext.charCodeAt(i + 2);
					string += String.fromCharCode(((c & 15) << 12) | ((c1 & 63) << 6) | (c2 & 63));
					i += 3;
				}
			}
			return string;
		}
	} (),
	base64Encode: function(input) {
		return _base64encode(input);
	},
	base64Decode: function(input) {
		return _base64decode(input);
	},
	isNumber: function(n) {
		return !isNaN(parseFloat(n)) && isFinite(n);
	},
	removeStyleAttribute: function(o, name, cssName) {
		if (o.style.removeProperty)
			o.style.removeProperty(cssName ? cssName : name)
		else if (o.style.removeAttribute)
			o.style.removeAttribute(name);
		else
			alert('You browser does not support removing style');
	},
	//data binding support ===========================================
	//it also provides an enclosure for holding data 
	_binder: function() {
		var jsdb_serverPage = String(); //to be set by the html page
		var jsdb_bind = 'jsdb';
		var jsdb_getdata = 'jsonDb_getData';
		var jsdb_putdata = 'jsonDb_putData';
		var const_userAlias = 'logonUserAlias';
		var e_onchange = 'onchange';
		var type_func = 'function';
		//
		//var _isIE = (navigator.appName == 'Microsoft Internet Explorer') || (navigator.appName == 'Netscape' && );
		var _isIE = ((navigator.appName == 'Microsoft Internet Explorer') || ((navigator.appName == 'Netscape') && (new RegExp("Trident/.*rv:([0-9]{1,}[\.0-9]{0,})").exec(navigator.userAgent) != null)));
		var _isFireFox = (navigator.userAgent.indexOf("Firefox") != -1);
		var _isChrome = (navigator.userAgent.toLowerCase().indexOf('chrome') > -1);
		var _isSafari = ((navigator.userAgent.toLowerCase().indexOf('safari') > -1) && !_isChrome);
		//
		if (!String.prototype.trim) {
			String.prototype.trim = function() {
				return this.replace(/^\s+|\s+$/g, '');
			}
		}
		//
		IsIE = function() {
			return _isIE;
		}
		IsFireFox = function() {
			return _isFireFox;
		}
		IsChrome = function() {
			return _isChrome;
		}
		IsSafari = function() {
			return _isSafari;
		}
		IsOpera = function() {
			return (typeof (window.opera) != 'undefined');
		}
		getEventSender = function(e) {
			var c;
			if (!e) e = window.event;
			if (e.target) c = e.target;
			else if (e.srcElement) c = e.srcElement;
			if (typeof c != 'undefined') {
				if (c.nodeType == 3)
					c = c.parentNode;
			}
			return c;
		}
		//
		var _windows = new Array();
		_addWindow = function(w) {
			var i = 0;
			while (i < _windows.length) {
				if (!_windows[i]) {
					_windows.splice(i, 1);
				}
				else if (_windows[i].closed) {
					_windows.splice(i, 1);
				}
				else {
					i++;
				}
			}
			_windows.push(w);
		}
		_getWindowById = function(pageId) {
			for (var i = 0; i < _windows.length; i++) {
				if (_windows[i]) {
					if (!_windows[i].closed) {
						if (_windows[i].document.pageId == pageId) {
							return _windows[i];
						}
					}
				}
			}
			if (JsonDataBinding.getChildWindowById) {
				return JsonDataBinding.getChildWindowById(pageId);
			}
		}
		_getWindowByPageFilename = function(pageFilename) {
			for (var i = 0; i < _windows.length; i++) {
				if (_windows[i]) {
					if (!_windows[i].closed) {
						if (JsonDataBinding.endsWithI(_windows[i].document.URL, pageFilename)) {
							return _windows[i];
						}
					}
				}
			}
		}
		//
		var _serverComponentName; //the name of server component made the callback for AJAX
		var _clientEventsHolder;
		_getClientEventHolder = function(eventName, objectName) {
			if (eventName && objectName) {
				if (!_clientEventsHolder) {
					_clientEventsHolder = {};
				}
				if (!_clientEventsHolder[eventName]) {
					_clientEventsHolder[eventName] = {};
				}
				var eh = _clientEventsHolder[eventName];
				if (!eh[objectName]) {
					eh[objectName] = {};
					eh[objectName].handlers = new Array();
				}
				return eh[objectName];
			}
		}
		_attachExtendedEvent = function(eventName, objectName, handler) {
			var eho = _getClientEventHolder(eventName, objectName);
			if (eho) {
				if (!eho.handlers) {
					eho.handlers = new Array();
				}
				var b = false;
				for (var i = 0; i < eho.handlers.length; i++) {
					if (eho.handlers[i] == handler) {
						b = true;
						break;
					}
				}
				if (!b) {
					eho.handlers.push(handler);
				}
			}
		}
		_detachExtendedEvent = function(eventName, objectName, handler) {
			var eho = _getClientEventHolder(eventName, objectName);
			if (eho && eho.handlers) {
				for (var i = 0; i < eho.handlers.length; i++) {
					if (eho.handlers[i] == handler) {
						eho.handlers.splice(i, 1);
						break;
					}
				}
			}
		}
		_clearExtendedEvent = function(eventName, objectName) {
			var eho = _getClientEventHolder(eventName, objectName);
			if (eho) {
				eho.handlers = new Array();
			}
		}
		_switchExtendedEvent = function(eventName, objectName, handler) {
			_clearExtendedEvent(eventName, objectName);
			_attachExtendedEvent(eventName, objectName, handler)
		}
		_fetchDetailRows = function(objectName) {
			var detailTbls = _getTableAttribute(objectName, 'LinkedDetails');
			if (detailTbls && sources) {
				if (sources[objectName] && sources[objectName].Rows && sources[objectName].Rows.length > 0 && sources[objectName].rowIndex >= 0) {
					//fetch details for all linked detail table
					for (var i = 0; i < detailTbls.length; i++) {
						if (sources[detailTbls[i].detailTableName] && detailTbls[i].dataset && detailTbls[i].dataset.length > sources[objectName].rowIndex && detailTbls[i].dataset[sources[objectName].rowIndex]) {
							//detail rows available
							sources[detailTbls[i].detailTableName].Rows = detailTbls[i].dataset[sources[objectName].rowIndex];
							sources[detailTbls[i].detailTableName].rowIndex = sources[detailTbls[i].detailTableName].Rows.length > 0 ? 0 : -1;
							bindTable(detailTbls[i].detailTableName, true);
						}
						else {
							//fetch details from server
							_setTableAttribute(detailTbls[i].detailTableName, 'LinkedMaster', detailTbls[i]);
							var rel = {};
							for (var k = 0; k < detailTbls[i].fields.length; k++) {
								rel[detailTbls[i].fields[k].code] = JsonDataBinding.columnValue(detailTbls[i].masterTableName, detailTbls[i].fields[k].name);
							}
							JsonDataBinding.executeServerMethod(detailTbls[i].masterMethod, rel);
						}
					}
				}
				else {
					//no data or no row selected, clear all details
					for (var i = 0; i < detailTbls.length; i++) {
						if (sources && sources[detailTbls[i].detailTableName]) {
							sources[detailTbls[i].detailTableName].Rows = [];
							sources[detailTbls[i].detailTableName].rowIndex = -1;
							bindTable(detailTbls[i].detailTableName, true);
						}
					}
				}
			}
		}
		_refetchDetailRows = function(objectName, detailTableName) {
			var detailTbls = _getTableAttribute(objectName, 'LinkedDetails');
			if (detailTbls && sources) {
				if (sources[objectName] && sources[objectName].Rows && sources[objectName].Rows.length > 0 && sources[objectName].rowIndex >= 0) {
					//fetch details for the linked detail table
					for (var i = 0; i < detailTbls.length; i++) {
						if (detailTbls[i].detailTableName == detailTableName) {
							//fetch details from server
							_setTableAttribute(detailTbls[i].detailTableName, 'LinkedMaster', detailTbls[i]);
							var rel = {};
							for (var k = 0; k < detailTbls[i].fields.length; k++) {
								rel[detailTbls[i].fields[k].code] = JsonDataBinding.columnValue(detailTbls[i].masterTableName, detailTbls[i].fields[k].name);
							}
							JsonDataBinding.executeServerMethod(detailTbls[i].masterMethod, rel);
						}
					}
				}
				else {
					//no data or no row selected, clear all details
					for (var i = 0; i < detailTbls.length; i++) {
						if (sources && sources[detailTbls[i].detailTableName]) {
							sources[detailTbls[i].detailTableName].Rows = [];
							sources[detailTbls[i].detailTableName].rowIndex = -1;
							bindTable(detailTbls[i].detailTableName, true);
						}
					}
				}
			}
		}
		_executeEventHandlers = function(eventName, objectName, data, attrs) {
			if (eventName == 'CurrentRowIndexChanged') {
				//fetch details
				_fetchDetailRows(objectName);
			}
			else if (eventName == 'DataArrived') {
				if (attrs && attrs.isFirstTime) {
					var detailTbls = _getTableAttribute(objectName, 'LinkedDetails');
					if (detailTbls) {
						for (var i = 0; i < detailTbls.length; i++) {
							detailTbls[i].dataset = null;
							if (sources && sources[detailTbls[i].detailTableName]) {
								sources[detailTbls[i].detailTableName].Rows = [];
								sources[detailTbls[i].detailTableName].rowIndex = -1;
								bindTable(detailTbls[i].detailTableName, true);
							}
						}
					}
				}
				//cache rows
				if (data) {
					var obj = data[objectName];
					if (obj && obj.fields) {
						var rel = _getTableAttribute(objectName, 'LinkedMaster');
						if (rel && sources[rel.masterTableName] && sources[rel.masterTableName].Rows && sources[rel.masterTableName].Rows.length > 0) {
							var masterRowIdx = sources[rel.masterTableName].rowIndex;
							var isCurrent = (masterRowIdx >= 0);
							if (isCurrent) {
								for (var k = 0; k < rel.fields.length; k++) {
									if (obj.fields[rel.fields[k].name] != JsonDataBinding.columnValue(rel.masterTableName, rel.fields[k].name)) {
										isCurrent = false;
										break;
									}
								}
							}
							if (isCurrent) {
								if (!rel.dataset) {
									rel.dataset = new Array(sources[rel.masterTableName].Rows.length);
								}
								rel.dataset[masterRowIdx] = sources[objectName].Rows;
							}
						}
					}
				}
			}
			var eho = _getClientEventHolder(eventName, objectName);
			if (eho && eho.handlers) {
				for (var i = 0; i < eho.handlers.length; i++) {
					if (eventName == 'DataUpdated' && data) {
						eho.handlers[i](null, data);
					}
					else {
						eho.handlers[i]();
					}
				}
			}
		}
		_getClientEventObject = function(eventName) {
			if (_clientEventsHolder && _serverComponentName) {
				var eh = _clientEventsHolder[eventName];
				if (eh && eh[_serverComponentName]) {
					return eh[_serverComponentName];
				}
			}
		}
		_executeClientEventObject = function(eventName) {
			var eho = _getClientEventObject(eventName);
			if (eho && eho.handlers) {
				for (var i = 0; i < eho.handlers.length; i++) {
					eho.handlers[i](JsonDataBinding.values.serverFailure);
				}
			}
		}
		var _objectProperties = {};
		_getObjectProperty = function(objectName, propertyName) {
			if (_objectProperties[objectName]) {
				var obj = _objectProperties[objectName];
				return obj[propertyName];
			}
		}
		_setObjectProperty = function(objectName, propertyName, value) {
			if (!_objectProperties[objectName]) {
				_objectProperties[objectName] = {};
			}
			var obj = _objectProperties[objectName];
			obj[propertyName] = value;
		}
		_onSetCustomValue = function(obj, valueName) {
			var dbs = obj.getAttribute(jsdb_bind);
			if (typeof dbs != 'undefined' && dbs != null && dbs != '') {
				var binds = dbs.split(';');
				for (var sIdx = 0; sIdx < binds.length; sIdx++) {
					var bind = binds[sIdx].split(':');
					var sourceName = bind[0];
					var tbl = sources[sourceName];
					if (typeof tbl != 'undefined') {
						var field = bind[1];
						var target = bind[2];
						if (valueName == target) {
							var rIdx;
							var rIdxs;
							if (typeof obj.jsdbRowIndex != 'undefined') {
								rIdxs = obj.jsdbRowIndex;
							}
							if (rIdxs) {
								rIdx = rIdxs[sourceName];
							}
							var rIdx0 = tbl.rowIndex;
							if (typeof rIdx == 'undefined') {
								rIdx = rIdx0;
							}
							if (rIdx >= 0 && rIdx < tbl.Rows.length) {
								_backupdata(tbl.TableName, rIdx);
								tbl.rowIndex = rIdx;
								preserveKeys(sourceName);
								var c = _columnNameToIndex(tbl.TableName, field);
								var v;
								v = obj[target];
								tbl.Rows[rIdx].ItemArray[c] = v;
								tbl.Rows[rIdx].changed = true;
								JsonDataBinding.onvaluechanged(tbl, rIdx, c, v);
								tbl.rowIndex = rIdx0;
							}
							break;
						}
					}
				}
			}
		}
		//
		var jsdb_cultureName = 'cultureName';
		var _cultureName = 'en';
		var resTable = {
			'TableName': '_pageResources_',
			'Columns': [{ 'Name': 'cultureName', 'ReadOnly': 'true', 'Type': 'string'}],
			'PrimaryKey': ['cultureName'],
			'DataRelations': [],
			'Rows': []
		};
		//
		var _datetimePicker;
		var _datetimeInputs;
		_getdatetimepicker = function() {
			return _datetimePicker;
		}
		_pushDatetimeInput = function(textBoxId) {
			if (!_datetimeInputs) {
				_datetimeInputs = new Array();
			}
			_datetimeInputs.push(textBoxId)
		}
		_setDatetimePicker = function(datetimePicker) {
			_datetimePicker = datetimePicker;
			if (_datetimePicker) {
				if (_datetimeInputs) {
					for (var i = 0; i < _datetimeInputs.length; i++) {
						JsonDataBinding.CreateDatetimePickerForTextBox(_datetimeInputs[i].textBoxId, _datetimeInputs[i].fontsize, _datetimeInputs[i].inputTime, _datetimeInputs[i].standalone, _datetimeInputs[i].container, _datetimeInputs[i].disableMove);
					}
					_datetimeInputs = null;
				}
			}
		}
		//
		//all tables
		var dataChangeHandlers = {};
		var sources = new Object();
		//table attributes
		var tableAttributes = new Object();
		//on row index change event handlers
		var handlersOnRowIndex = new Object();
		var onrowdeletehandlers = new Object();
		//
		var hasActivity = false;
		var activityWatcher;
		activity = function() {
			//JsonDataBinding.ShowDebugInfoLine('check activity');
			var u = JsonDataBinding.getCookie(const_userAlias);
			if (typeof u != 'undefined' && u != null) {
				if (u.length > 0) {
					//JsonDataBinding.ShowDebugInfoLine('has activity:'+hasActivity);
					if (hasActivity) {
						hasActivity = false;
						var uu = u.split(' ');
						if (uu.length > 3) {
							JsonDataBinding.setCookie(const_userAlias, u, uu[3]);
						}
					}
					activityWatcher = setTimeout(activity, 3000);
				}
				else {
					//JsonDataBinding.ShowDebugInfoLine('login empty');
					window.location.reload();
				}
			}
			else {
				//JsonDataBinding.ShowDebugInfoLine('login not found');
				window.location.reload();
			}
		}
		var _sessionWatcher = null;
		var _sessionTimeout = 20; //minutes
		//var _sessionVariableNames = new Array();
		_startSessionWatcher = function() {
			if (!_sessionWatcher) {
				_sessionWatcher = setTimeout(_sessionKeepAlive, _sessionTimeout * 3000);
			}
		}
		_sessionKeepAlive = function() {
			var vs = _getSessionVariables();
			if (vs != null && vs.length > 0) {
				//JsonDataBinding.ShowDebugInfoLine('global variables:'+vs.length);
				for (var i = 0; i < vs.length; i++) {
					//var v = JsonDataBinding.getCookie(_sessionVariableNames[i]);
					//JsonDataBinding.setCookie(_sessionVariableNames[i], v, _sessionTimeout);
					//JsonDataBinding.ShowDebugInfoLine('global variables['+i+']:'+vs[i].name+'='+vs[i].value);
					JsonDataBinding.setCookie(vs[i].name, vs[i].value, _sessionTimeout);
				}
				_sessionWatcher = setTimeout(_sessionKeepAlive, _sessionTimeout * 3000);
			}
			else {
				//JsonDataBinding.ShowDebugInfoLine('no global variables');
				_sessionWatcher = null;
			}
		}
		_setSessionTimeout = function(tm) {
			if (tm >= 1) {
				_sessionTimeout = tm;
			}
		}
		_getSessionTimeout = function() {
			return _sessionTimeout;
		}
		_sessionVariableExists = function(variableName) {
			return JsonDataBinding.cookieExists(variableName);
		}
		_setSessionVariable = function(variableName, value) {
			JsonDataBinding.setCookie(variableName, value, _sessionTimeout);
			_startSessionWatcher()
		}
		_initSessionVariable = function(variableName, value) {
			if (!JsonDataBinding.cookieExists(variableName)) {
				JsonDataBinding.setCookie(variableName, value, _sessionTimeout);
			}
			_startSessionWatcher();
		}
		_getSessionVariable = function(variableName) {
			var v = JsonDataBinding.getCookie(variableName);
			return v;
		}
		_eraseSessionVariable = function(variableName) {
			JsonDataBinding.eraseCookie(variableName);
		}
		_getSessionVariables = function () {
			var aret = new Array();
			if (JsonDataBinding.UseLocalStore) {
				if (window.localStorage) {
					var gv = window.localStorage['limnor_gv'];
					if (gv && gv.length > 0) {
						var vs = JSON.parse(gv);
						for (var nm in vs) {
							if (nm != const_userAlias) {
								var vl = vs[nm];
								if (typeof (vl) != 'undefined' && vl !== null) {
									if (typeof (vl.e) != 'undefined' && vl.e !== null) {
										if (vl.e < new Date()) {
											delete vs[nm];
											gv = JSON.stringify(vs);
											window.localStorage['limnor_gv'] = gv;
											continue;
										}
									}
									aret.push({ name: nm, value: vl.v });
								}
							}
						}
					}
				}
			}
			else {
				var ca = document.cookie.split(';');
				for (var i = 0; i < ca.length; i++) {
					var c = ca[i];
					var pos = c.indexOf('=');
					if (pos > 0) {
						var nm = c.substr(0, pos).replace(/^\s+|\s+$/g, "");
						if (nm != const_userAlias) {
							var o = { 'name': nm, 'value': c.substr(pos + 1) };
							aret.push(o);
						}
					}
				}
			}
			return aret;
		}
		_addPageCulture = function(cultureName) {
			if (typeof cultureName == 'undefined' || cultureName == null) {
				cultureName = '';
			}
			var rowName = 'row_' + cultureName.replace('-', '_');
			var r = eval(rowName);
			var idx = resTable.Rows.push(r) - 1;
			if (sources[resTable.TableName]) {
				sources[resTable.TableName].rowIndex = idx;
				_onRowIndexChange(resTable.TableName);
			}
			else {
				var v = new Object();
				v.Tables = new Array();
				v.Tables.push(resTable);
				_setDataSource.call(v);
			}
		}
		_getValueInCurrentCulture = function(valueName) {
			if (typeof valueName != 'undefined' && valueName != null) {
				valueName = valueName.toLowerCase();
				var idx = sources[resTable.TableName] ? sources[resTable.TableName].rowIndex : 0;
				if (idx < 0 || idx >= resTable.Rows.length) {
					idx = 0;
				}
				if (idx < resTable.Rows.length) {
					for (var c = 0; c < resTable.Columns.length; c++) {
						if (valueName == resTable.Columns[c].Name) {
							return resTable.Rows[idx].ItemArray[c];
						}
					}
				}
			}
		}
		_setCulture = function(cultureName) {
			_cultureName = cultureName;
			if (typeof _cultureName == 'undefined' || _cultureName == null) {
				_cultureName = '';
			}
			JsonDataBinding.setCookie(jsdb_cultureName, cultureName, 99999);
			var idx = -1;
			for (var i = 0; i < resTable.Rows.length; i++) {
				if (resTable.Rows[i].ItemArray[0] == cultureName) {
					idx = i;
					break;
				}
			}
			if (idx < 0) {
				var sPath = window.location.pathname;
				var sPage = sPath.substring(sPath.lastIndexOf('/') + 1);
				sPage = sPage.substring(0, sPage.lastIndexOf('.'));
				var element1 = document.createElement('script');
				if (_cultureName == '') {
					element1.src = 'libjs/' + sPage + '.js';
				}
				else {
					element1.src = cultureName + '/' + sPage + '.js';
				}
				element1.type = 'text/javascript';
				element1.async = false;
				document.getElementsByTagName('head')[0].appendChild(element1);
			}
			else {
				sources[resTable.TableName].rowIndex = idx;
				_onRowIndexChange(resTable.TableName);
			}
		}
		_getCulture = function() {
			return JsonDataBinding.getCookie(jsdb_cultureName);
		}
		_addPageResourceName = function(resName, resType) {
			resTable.Columns.push({ 'Name': resName, 'ReadOnly': 'true', 'Type': resType });
		}
		_setUserLogCookieName = function(nm) {
			const_userAlias = nm;
		}
		_getloginValue = function(vid, defVal) {
			if (const_userAlias == 'logonUserAlias') {
				var u = JsonDataBinding.getCookieByStartsWith('WebLgin');
				if (u.length > 0) {
					var uu = u[0].value.split(' ');
					if (uu.length > vid) {
						return uu[vid];
					}
				}
			}
			else {
				var u = JsonDataBinding.getCookie(const_userAlias);
				if (typeof u != 'undefined' && u != null) {
					if (u.length > 0) {
						var uu = u.split(' ');
						if (uu.length > vid) {
							return uu[vid];
						}
					}
				}
			}
			return defVal;
		}
		_getCurrentUserAlias = function() {
			var u = _getloginValue(0, null);
			if (u != null) {
				u = JsonDataBinding.replaceAll(u, '#nbsp#', ' ', false);
			}
			return u;
		}
		_getCurrentUserLevel = function() {
			return _getloginValue(1, -1);
		}
		_getCurrentUserID = function() {
			return _getloginValue(2, 0);
		}
		_userLoggedOn = function() {
			if (const_userAlias == 'logonUserAlias') {
				var u = JsonDataBinding.getCookieByStartsWith('WebLgin');
				return (u.length > 0);
			}
			else {
				var u = JsonDataBinding.getCookie(const_userAlias);
				if (typeof u != 'undefined' && u != null) {
					if (u.length > 0) {
						return true;
					}
				}
			}
			return false;
		}
		//
		var _eventFirer;
		_setEventFirer = function(eo) {
			_eventFirer = eo;
		}
		//
		_setServerPage = function(pageUrl) {
			jsdb_serverPage = pageUrl;
		}
		_getServerPage = function() {
			return jsdb_serverPage;
		}
		_addOnRowIndexChangeHandler = function(tableName, handler) {
			if (typeof handlersOnRowIndex == 'undefined' || handlersOnRowIndex == null) {
				handlersOnRowIndex = new Object();
			}
			if (typeof handlersOnRowIndex[tableName] == 'undefined') {
				handlersOnRowIndex[tableName] = new Array();
			}
			handlersOnRowIndex[tableName].push(handler);
		}
		//returns 0: not logged on; 1: logged on and level fail; 2: log on and level is fine.
		_hasLoggedOn = function() {
			if (const_userAlias == 'logonUserAlias') {
				var u = JsonDataBinding.getCookieByStartsWith('WebLgin');
				if (u.length > 0) {
					return 2;
				}
			}
			else {
				var u = JsonDataBinding.getCookie(const_userAlias);
				if (typeof u != 'undefined' && u != null) {
					if (u.length > 0) {
						if (typeof JsonDataBinding.TargetUserLevel != 'undefined' && JsonDataBinding.TargetUserLevel != null && JsonDataBinding.TargetUserLevel >= 0) {
							var uu = u.split(' ');
							if (uu.length > 1) {
								if (parseInt(uu[1]) <= JsonDataBinding.TargetUserLevel) {
									return 2; //user level OK
								}
								else {
									return 1; //user level not match
								}
							}
							else {
								return 1; //user level not present
							}
						}
						else {
							return 2; //not use user level
						}
					}
				}
			}
			return 0; //not logged in
		}
		_logOff = function() {
			if (typeof activityWatcher != 'undefined' && activityWatcher != null) {
				clearTimeout(activityWatcher);
			}
			activityWatcher = null;
			if (const_userAlias == 'logonUserAlias') {
				var u = JsonDataBinding.getCookieByStartsWith('WebLgin');
				for (var i = 0; i < u.length; i++) {
					JsonDataBinding.eraseCookie(u[i].name);
				}
				window.location.reload();
			}
			else {
				var u = JsonDataBinding.getCookie(const_userAlias);
				if (typeof u != 'undefined' && u != null) {
					if (u.length > 0) {
						JsonDataBinding.eraseCookie(const_userAlias);
						window.location.reload();
					}
				}
			}
		}
		_loginPassed = function(login, expire, userLevel, userid) {
			login = JsonDataBinding.replaceAll(login, ' ', '#nbsp#', false);
			if (userLevel) {
				if (expire) {
					if (userid) {
						JsonDataBinding.setCookie(const_userAlias, login + ' ' + userLevel + ' ' + userid + ' ' + expire, expire);
					}
					else {
						JsonDataBinding.setCookie(const_userAlias, login + ' ' + userLevel + ' 0 ' + expire, expire);
					}
				}
				else {
					if (userid) {
						JsonDataBinding.setCookie(const_userAlias, login + ' ' + userLevel + ' ' + userid, null);
					}
					else {
						JsonDataBinding.setCookie(const_userAlias, login + ' ' + userLevel + ' 0', null);
					}
				}
			}
			else {
				if (expire) {
					if (userid) {
						JsonDataBinding.setCookie(const_userAlias, login + ' 0 ' + userid + ' ' + expire, expire);
					}
					else {
						JsonDataBinding.setCookie(const_userAlias, login + ' 0 0 ' + expire, expire);
					}
				}
				else {
					if (userid) {
						JsonDataBinding.setCookie(const_userAlias, login + ' 0 ' + userid, null);
					}
					else {
						JsonDataBinding.setCookie(const_userAlias, login + ' 0 0', null);
					}
				}
			}
			_setupLoginWatcher();
			_executeClientEventObject('UserLogin');
		}

		function addloader(func) {
			var oldonload = window.onload;
			if (typeof window.onload != 'function') {
				window.onload = func;
			} else {
				window.onload = function() {
					if (oldonload) {
						oldonload();
					}
					func();
				}
			}
		}
		function addMouseWatcher(func) {
			var oldonload = document.body.onmousemove;
			if (typeof document.body.onmousemove != 'function') {
				document.body.onmousemove = func;
			}
			else {
				document.body.onmousemove = function() {
					if (oldonload) {
						oldonload();
					}
					func();
				}
			}
		}
		function addKeyboardWatcher(func) {
			var oldonload = document.body.onkeydown;
			if (typeof document.body.onkeydown != 'function') {
				document.body.onkeydown = func;
			}
			else {
				document.body.onkeydown = function() {
					if (oldonload) {
						oldonload();
					}
					func();
				}
			}
		}
		_setupLoginWatcher = function() {
			var u = JsonDataBinding.getCookie(const_userAlias);
			if (typeof u == 'undefined' || u == null) {
				return;
			}
			if (u.length == 0) {
				return;
			}
			addKeyboardWatcher(function() { hasActivity = true; });
			addMouseWatcher(function() { hasActivity = true; });
			activityWatcher = setTimeout(activity, 3000);
		}
		_columnNameToIndex = function(tablename, columnname) {
			if (sources[tablename]) {
				return sources[tablename].columnIndexes[columnname.toLowerCase()];
			}
		}
		//v is a JsonDataSet
		_setDataSource = function(dataAttrs) {
			var v = this; //it can be a JsonDataSet or a WebRequestOrResponse 
			if (typeof v != 'undefined' && v != null && typeof v.Data != 'undefined') {
				v = v.Data;
			}
			if (typeof v != 'undefined' && v != null && typeof v.Tables != 'undefined' && v.Tables != null) {
				var name;
				var dataIgnore = {};
				for (var i = 0; i < v.Tables.length; i++) {
					name = v.Tables[i].TableName;
					var isFirstTime = true;
					var isDataStreaming = false; //
					var streamStatus;
					var utc = _getTableAttribute(name, 'AutoConvertUTCforWeb');
					if (dataAttrs && dataAttrs[name] && dataAttrs[name].streamId) {
						isDataStreaming = true;
						streamStatus = JsonDataBinding.getTableAttribute(name, 'batchStatus');
						if (!streamStatus) {
							streamStatus = {};
							streamStatus.streamId = dataAttrs[name].streamId;
							streamStatus.functionName = dataAttrs[name].functionName;
							JsonDataBinding.setTableAttribute(name, 'batchStatus', streamStatus);
						}
						if (!streamStatus.batchSize) {
							if (v.Tables[i].Rows.length > 0) {
								streamStatus.batchSize = v.Tables[i].Rows.length;
							}
							else {
								streamStatus.batchSize = 100;
							}
						}
						if (dataAttrs[name].isFirstBatch) {
							streamStatus.streamId = dataAttrs[name].streamId;
							streamStatus.functionName = dataAttrs[name].functionName;
						}
						else {
							if (streamStatus.functionName != dataAttrs[name].functionName || streamStatus.streamId != dataAttrs[name].streamId) {
								dataIgnore[name] = true;
							}
							isFirstTime = false;
						}
						if (!dataIgnore[name]) {
							streamStatus.batchKey = dataAttrs[name].batchKey;
							streamStatus.serverComponentName = dataAttrs[name].serverComponentName;
							streamStatus.parameters = dataAttrs[name].parameters;
						}
					}
					else {
						if (JsonDataBinding.values.isdatastreaming && JsonDataBinding.values.isdatastreaming.length > 0) {
							for (var k = 0; k < JsonDataBinding.values.isdatastreaming.length; k++) {
								if (JsonDataBinding.values.isdatastreaming[k] == name) {
									isDataStreaming = true;
									isFirstTime = false;
									break;
								}
							}
						}
						if (!isDataStreaming) {
							var dstreaming = _getTableAttribute(name, 'isDataStreaming')
							if (typeof dstreaming != 'undefined' && dstreaming != null) {
								isDataStreaming = dstreaming;
								var isf = _getTableAttribute(name, 'isFisrtTime')
								if (typeof isf != 'undefined' && isf != null) {
									isFirstTime = isf;
								}
							}
						}
					}
					if (!dataIgnore[name]) {
						var j, r;
						var hasBlob = false;
						var blobFields = new Array();
						var isFieldImages = JsonDataBinding.getObjectProperty(name, 'IsFieldImage');
						for (j = 0; j < v.Tables[i].Columns.length; j++) {
							if (v.Tables[i].Columns[j].Type == 252) {
								if (isFieldImages && isFieldImages.length > j) {
									if (isFieldImages[j]) {
										continue;
									}
								}
								hasBlob = true;
								blobFields.push(j);
							}
						}
						if (hasBlob || utc) {
							for (r = 0; r < v.Tables[i].Rows.length; r++) {
								if (hasBlob) {
									for (j = 0; j < blobFields.length; j++) {
										v.Tables[i].Rows[r].ItemArray[blobFields[j]] = JsonDataBinding.decodeBase64(v.Tables[i].Rows[r].ItemArray[blobFields[j]]);
									}
								}
								if (utc) {
									for (j = 0; j < v.Tables[i].Columns.length; j++) {
										if (v.Tables[i].Columns[j].Type == 12) {
											v.Tables[i].Rows[r].ItemArray[j] = JsonDataBinding.datetime.toLocalDate(v.Tables[i].Rows[r].ItemArray[j]);
										}
									}
								}
							}
						}
						if (isDataStreaming && !isFirstTime && sources[name]) {
							if (sources[name].Rows) {
								sources[name].newRowStartIndex = sources[name].Rows.length;
							}
							else {
								sources[name].Rows = new Array();
								sources[name].newRowStartIndex = 0;
							}
							//sources[name].rowIndex = sources[name].newRowStartIndex;
							for (r = 0; r < v.Tables[i].Rows.length; r++) {
								sources[name].Rows.push(v.Tables[i].Rows[r]);
							}
						}
						else {
							sources[name] = v.Tables[i];
							sources[name].columnIndexes = new Object(); //Name:index mapping
							sources[name].rowIndex = 0;
							//create column name<=>ordinal mapping
							for (j = 0; j < sources[name].Columns.length; j++) {
								sources[name].columnIndexes[sources[name].Columns[j].Name.toLowerCase()] = j;
								sources[name].columnIndexes[sources[name].Columns[j].Name] = j;
							}
							//set read-only fields
							var readonlyfields = _getTableAttribute(name, 'readonlyfields');
							if (readonlyfields && readonlyfields.length > 0) {
								for (j = 0; j < readonlyfields.length; j++) {
									var jc0 = sources[name].columnIndexes[readonlyfields[j]];
									if (jc0 >= 0 && jc0 < sources[name].Columns.length) {
										sources[name].Columns[jc0].ReadOnly = true;
									}
								}
							}
						}
					}
				}
				for (var k = 0; k < v.Tables.length; k++) {
					name = v.Tables[k].TableName;
					if (!dataIgnore[name]) {
						_setTableAttribute(name, 'IsDataReady', false);
						bindTable(name, isFirstTime, isDataStreaming);
						_executeEventHandlers('DataArrived', name, dataAttrs, { isFirstTime: isFirstTime });
						if (isFirstTime || !isDataStreaming) {
							if (v.Tables[k].Rows && v.Tables[k].Rows.length > 0) {
								_executeEventHandlers('CurrentRowIndexChanged', name);
							}
						}
						_setTableAttribute(name, 'IsDataReady', true);
					}
				}
				for (var k = 0; k < v.Tables.length; k++) {
					name = v.Tables[k].TableName;
					if (!dataIgnore[name]) {
						if (dataAttrs && dataAttrs[name] && dataAttrs[name].streamId) {
							streamStatus = JsonDataBinding.getTableAttribute(name, 'batchStatus');
							if (streamStatus && streamStatus.batchKey && v.Tables[k].Rows.length >= streamStatus.batchSize) {
								//fetch next batch
								var obj = {};
								if (dataAttrs[name].uploadedValues) {
									for (var nm in dataAttrs[name].uploadedValues) {
										if (dataAttrs[name].uploadedValues.hasOwnProperty(nm)) {
											obj[nm] = dataAttrs[name].uploadedValues[nm];
										}
									}
								}
								if (streamStatus.parameters) {
									for (var nm in streamStatus.parameters) {
										if (streamStatus.parameters.hasOwnProperty(nm)) {
											obj[nm] = streamStatus.parameters[nm];
										}
									}
								}
								if (dataAttrs[name].batchWhere) {
									obj.batchWhere = dataAttrs[name].batchWhere;
								}
								if (dataAttrs[name].batchWhereParams) {
									obj.batchparameters = dataAttrs[name].batchWhereParams;
								}
								obj.batchStreamId = streamStatus.streamId;
								obj.serverComponentName = streamStatus.serverComponentName;
								if (streamStatus.batchDelay > 0) {
									function fetchnextbatch() {
										_executeServerCommands([{ method: streamStatus.functionName, value: streamStatus.batchKey}], obj, null, { background: true });
									}
									setTimeout(fetchnextbatch, streamStatus.batchDelay);
								}
								else {
									_executeServerCommands([{ method: streamStatus.functionName, value: streamStatus.batchKey}], obj, null, { background: true });
								}
							}
						}
					}
				}
			}
		}
		isDataSourceValid = function (sourceName) {
			return (sources && sources[sourceName]);
		};
		bindData = function (e, name, firstTime, isDataStreaming) {
			for (var i = 0; i < e.childNodes.length; i++) {
				var a = e.childNodes[i];
				if (typeof a != 'undefined' && a != null) {
					var pchld = false;
					if (typeof a.getAttribute != 'undefined') {
						var bd = a.getAttribute(jsdb_bind);
						if (typeof bd != 'undefined' && bd != null && bd != '') {
							var binds = bd.split(';');
							for (var sIdx = 0; sIdx < binds.length; sIdx++) {
								var bind = binds[sIdx].split(':');
								var dbTable = bind[0];
								if (dbTable == name) {
									if (a.isTreeView) {
										if (firstTime || isDataStreaming) {
											a.jsData.onDataReady(sources[name]);
										}
										else {
											a.jsData.onRowIndexChange(name);
										}
									}
									else {
										if (bind.length == 1) {
											if (firstTime) {
												if (a.IsDataRepeater) {
													if (typeof a.jsData == 'undefined') {
														a.jsData = JsonDataBinding.DataRepeater(a, sources[name]);
													}
													else {
														a.jsData.onDataReady(sources[name]);
													}
												}
												else {
													if (typeof a.tagName != 'undefined' && a.tagName != null) {
														if (a.tagName.toLowerCase() == "table") {
															if (a.chklist) {
																a.chklist.loadRecords(sources[name].Rows);
																a.chklist.setMessage('');
																a.chklist.applyTargetdata();
															}
															else {
																if (typeof a.jsData == 'undefined') {
																	a.jsData = JsonDataBinding.HtmlTableData(a, sources[name]);
																}
																else {
																	a.jsData.onDataReady(sources[name]);
																}
															}
														}
													}
												}
											}
											else {
												if (a.IsDataRepeater) {
													if (a.jsData) {
														a.jsData.onPageIndexChange(name);
													}
												}
												else {
													if (typeof a.tagName != 'undefined' && a.tagName != null) {
														if (a.tagName.toLowerCase() == "table") {
															if (typeof a.jsData != 'undefined' && a.jsData != null) {
																a.jsData.onRowIndexChange(name);
															}
														}
													}
												}
											}
										}
										else if (bind.length == 3) {
											var isListbox = false;
											var isfieldset = false;
											if (typeof a.tagName != 'undefined' && a.tagName != null) {
												var tag = a.tagName.toLowerCase();
												isfieldset = (tag == 'fieldset');
												isListbox = (tag == "select");
											}
											if (isListbox) {
												var itemField = bind[1];
												var valueField = bind[2];
												var itemFieldIdx = -1;
												var valueFieldIdx = -1;
												for (var c = 0; c < sources[name].Columns.length; c++) {
													if (sources[name].Columns[c].Name == itemField) {
														itemFieldIdx = c;
													}
													if (sources[name].Columns[c].Name == valueField) {
														valueFieldIdx = c;
													}
													if (valueFieldIdx >= 0 && itemFieldIdx >= 0) {
														break;
													}
												}
												if (valueFieldIdx < 0) {
													if (itemFieldIdx >= 0) {
														valueFieldIdx = itemFieldIdx;
													}
												}
												if (itemFieldIdx < 0) {
													if (valueFieldIdx >= 0) {
														itemFieldIdx = valueFieldIdx;
													}
												}
												if (itemFieldIdx >= 0) {
													if (firstTime) {
														if (typeof a.jsData == 'undefined') {
															a.jsData = JsonDataBinding.HtmlListboxData(a, sources[name], itemFieldIdx, valueFieldIdx);
														}
														else {
															a.jsData.onDataReady(sources[name]);
														}
													}
													else {
														if (typeof a.jsData != 'undefined' && a.jsData != null) {
															a.jsData.onRowIndexChange(name);
														}
													}
												}
											}
											else {
												var field = bind[1];
												var target = bind[2];
												var ci = _columnNameToIndex(name, field);
												var b = (typeof a.disableMonitor == 'undefined') ? false : a.disableMonitor;
												a.disableMonitor = true;
												if (sources[name].rowIndex >= 0 && sources[name].rowIndex < sources[name].Rows.length) {
													if (target == 'innerText') {
														JsonDataBinding.SetInnerText(a, sources[name].Rows[sources[name].rowIndex].ItemArray[ci]);
													}
													else if (target == 'ImageData') {
														JsonDataBinding.SetImageData(a, sources[name].Rows[sources[name].rowIndex].ItemArray[ci]);
													}
													else {
														var lg = null;
														if (isfieldset) {
															for (var ai = 0; ai < a.children.length; ai++) {
																if (a.children[ai].tagName.toLowerCase() == 'legend') {
																	lg = a.children[ai];
																	break;
																}
															}
														}
														if (sources[name].Rows[sources[name].rowIndex].ItemArray[ci] == null) {
															a[target] = JsonDataBinding.NullDisplay;
															a.nullDisplayEmpty = true;
															if (lg) {
																lg.innerHTML = '';
															}
														}
														else {
															var v0 = JsonDataBinding.toText(sources[name].Rows[sources[name].rowIndex].ItemArray[ci]);
															a[target] = v0;
															if (lg) {
																lg.innerHTML = v0;
															}
														}
													}
													a.val = JsonDataBinding.toText(sources[name].Rows[sources[name].rowIndex].ItemArray[ci]);
												}
												else {
													if (target == 'innerText') {
														JsonDataBinding.SetInnerText(a, '');
													}
													else if (target == 'ImageData') {
														JsonDataBinding.SetImageData(a);
													}
													else {
														a[target] = '';
													}
													a.val = '';
												}
												a.disableMonitor = b;
												if (typeof a.jsdbRowIndex == 'undefined') {
													a.jsdbRowIndex = {}
												}
												a.jsdbRowIndex[name] = sources[name].rowIndex;
												if (firstTime) {
													//setup modification watcher
													var tag = a.tagName.toLowerCase();
													if (target == 'innerHTML') {
														if (tag == 'div') {
															a.oninnerHtmlChanged = changeBoundData;
															JsonDataBinding.addTextBoxObserver(a);
														}
													}
													else if (tag == 'input' && target == 'checked') {
														a.isCheckBox = true;
														JsonDataBinding.AttachEvent(a, 'onclick', changeBoundData);
														a.onCheckedChanged = changeBoundData;
														JsonDataBinding.addTextBoxObserver(a);
													}
													//else if (isEventSupported(a, e_onchange)) {
													else if (tag == 'input' || (tag == 'textarea')) {
														JsonDataBinding.AttachEvent(a, e_onchange, changeBoundData);
														a.onsetbounddata = changeBoundData;
														JsonDataBinding.addTextBoxObserver(a);
													}
												}
												if (isfieldset) {
													bindData(a, name, firstTime, isDataStreaming);
													pchld = true;
												}
											}
										}
									}
								}
							}
						}
						else {
							bindData(a, name, firstTime, isDataStreaming);
							pchld = true;
						}
					}
					if (name == '_pageResources_') {
						if (a.ActControls && a.ActControls.length > 0) {
							var ps = { childNodes: new Array() };
							for (var k = 0; k < a.ActControls.length; k++) {
								if (a.ActControls[k]) {
									var c = document.getElementById(a.ActControls[k]);
									if (c) {
										var pexists = false;
										for (var k2 = 0; k2 < ps.childNodes.length; k2++) {
											if (ps.childNodes[k2] === c) {
												pexists = true;
												break;
											}
										}
										if (!pexists) {
											ps.childNodes.push(c);
										}
									}
								}
							}
							bindData(ps, name, firstTime, isDataStreaming);
						}
						else {
							if (!pchld) {
								if (a.tagName && a.tagName.toLowerCase() == 'div') {
									bindData(a, name, firstTime, isDataStreaming);
								}
							}
						}
					}
				}
			}
		};
		function bindTable(name, firstTime, isDataStreaming) {
			bindData(document.body, name, firstTime, isDataStreaming);
		}
		function refreshTableBindColumnDisplay(e, name, rowidx, colIdx) {
			for (var i = 0; i < e.childNodes.length; i++) {
				var a = e.childNodes[i];
				if (typeof a != 'undefined' && a != null) {
					if (typeof a.getAttribute != 'undefined') {
						var bd = a.getAttribute(jsdb_bind);
						if (typeof bd != 'undefined' && bd != null && bd != '') {
							var binds = bd.split(';');
							for (var sIdx = 0; sIdx < binds.length; sIdx++) {
								var bind = binds[sIdx].split(':');
								var dbTable = bind[0];
								if (dbTable == name) {
									if (a.jsData && a.jsData.refreshBindColumnDisplay) {
										a.jsData.refreshBindColumnDisplay(name, rowidx, colIdx);
									}
									else if (bind.length == 3) {
										if (rowidx == sources[name].rowIndex) {
											var field = bind[1];
											var target = bind[2];
											var ci = _columnNameToIndex(name, field);
											var b = (typeof a.disableMonitor == 'undefined') ? false : a.disableMonitor;
											a.disableMonitor = true;
											if (rowidx < sources[name].Rows.length) {
												if (target == 'innerText') {
													JsonDataBinding.SetInnerText(a, sources[name].Rows[rowidx].ItemArray[ci]);
												}
												else if (target == 'ImageData') {
													JsonDataBinding.SetImageData(a, sources[name].Rows[rowidx].ItemArray[ci]);
												}
												else {
													if (sources[name].Rows[rowidx].ItemArray[ci] == null) {
														a[target] = JsonDataBinding.NullDisplay;
														a.nullDisplayEmpty = true;
													}
													else {
														a[target] = JsonDataBinding.toText(sources[name].Rows[rowidx].ItemArray[ci]);
													}
												}
												a.val = JsonDataBinding.toText(sources[name].Rows[rowidx].ItemArray[ci]);
											}
											a.disableMonitor = b;
										}
									}
								}
							}
						}
						else {
							refreshTableBindColumnDisplay(a, name, rowidx, colIdx);
						}
					}
				}
			}
		}
		function refreshBindColumnDisplay(name, rowidx, colIdx) {
			refreshTableBindColumnDisplay(document.body, name, rowidx, colIdx);
		}
		function getNextRowIndex(name, currentIndex) {
			var idx2 = -1;
			var idx = currentIndex + 1;
			while (idx < sources[name].Rows.length) {
				if (!sources[name].Rows[idx].deleted && !sources[name].Rows[idx].removed) {
					idx2 = idx;
					break;
				}
				idx++;
			}
			return idx2;
		}
		function getPreviousRowIndex(name, currentIndex) {
			var idx2 = -1;
			var idx = currentIndex - 1;
			while (idx >= 0) {
				if (!sources[name].Rows[idx].deleted && !sources[name].Rows[idx].removed) {
					idx2 = idx;
					break;
				}
				idx--;
			}
			return idx2;
		}
		function onRowIndexChange(name) {
			if (typeof sources != 'undefined' && typeof sources[name] != 'undefined') {
				if (typeof handlersOnRowIndex != 'undefined') {
					if (handlersOnRowIndex[name] != null) {
						for (var i = 0; i < handlersOnRowIndex[name].length; i++) {
							handlersOnRowIndex[name][i](sources[name]);
						}
					}
				}
				_executeEventHandlers('CurrentRowIndexChanged', name);
			}
		}
		_bindData = function(e, name, firstTime, isDataStreaming) {
			bindData(e, name, firstTime, isDataStreaming);
		}
		_onRowIndexChange = function(name) {
			bindData(document.body, name, false);
			onRowIndexChange(name);
		}
		//it does not skip deleted records as other functions do
		_dataMoveToRecord = function(name, rowIndex) {
			if (sources && sources[name] && rowIndex >= 0 && rowIndex < sources[name].Rows.length) {
				JsonDataBinding.pollModifications();
				sources[name].rowIndex = rowIndex;
				_onRowIndexChange(name);
				return true;
			}
			return false;
		}
		_dataMoveFirst = function(name) {
			if (sources && sources[name]) {
				JsonDataBinding.pollModifications();
				var idx2 = getNextRowIndex(name, -1);
				if (idx2 >= 0) {
					sources[name].rowIndex = idx2;
					_onRowIndexChange(name);
					return true;
				}
			}
			return false;
		}
		_dataMoveLast = function(name) {
			if (sources && sources[name]) {
				JsonDataBinding.pollModifications();
				var idx2 = getPreviousRowIndex(name, sources[name].Rows.length);
				if (idx2 >= 0) {
					sources[name].rowIndex = idx2;
					_onRowIndexChange(name);
					return true;
				}
			}
			return false;
		}
		_dataMoveNext = function(name) {
			if (sources && typeof sources[name] != 'undefined' && sources[name].rowIndex < sources[name].Rows.length - 1) {
				JsonDataBinding.pollModifications();
				var idx2 = getNextRowIndex(name, sources[name].rowIndex);
				if (idx2 >= 0) {
					sources[name].rowIndex = idx2;
					_onRowIndexChange(name);
					return true;
				}
			}
			return false;
		}
		_dataMovePrevious = function(name) {
			if (sources && typeof sources[name] != 'undefined' && sources[name].rowIndex < sources[name].Rows.length && sources[name].rowIndex > 0) {
				JsonDataBinding.pollModifications();
				var idx2 = getPreviousRowIndex(name, sources[name].rowIndex);
				if (idx2 >= 0) {
					sources[name].rowIndex = idx2;
					_onRowIndexChange(name);
					return true;
				}
			}
			return false;
		}
		_clearTableRows = function(name) {
			if (sources && typeof sources[name] != 'undefined') {
				sources[name].Rows = [];
				sources[name].rowIndex = -1;
				bindData(document.body, name, true, false);
			}
		}
		_getModifiedRowCount = function(name) {
			JsonDataBinding.pollModifications();
			var r0 = 0;
			if (sources && sources[name]) {
				if (typeof sources[name] != 'undefined') {
					for (var r = 0; r < sources[name].Rows.length; r++) {
						if (sources[name].Rows[r].changed) {
							r0++;
						}
					}
				}
			}
			return r0;
		}
		_getDeletedRowCount = function(name) {
			var r0 = 0;
			if (sources && sources[name]) {
				if (typeof sources[name] != 'undefined') {
					for (var r = 0; r < sources[name].Rows.length; r++) {
						if (sources[name].Rows[r].deleted) {
							r0++;
						}
					}
				}
			}
			return r0;
		}
		_getActiveRowCount = function(name) {
			var r0 = 0;
			if (sources && sources[name]) {
				if (typeof sources[name] != 'undefined') {
					r0 = sources[name].Rows.length;
					for (var r = 0; r < sources[name].Rows.length; r++) {
						if (sources[name].Rows[r].deleted) {
							r0--;
						}
					}
				}
			}
			return r0;
		}
		_getNewRowCount = function(name) {
			var r0 = 0;
			if (sources && sources[name]) {
				for (var r = 0; r < sources[name].Rows.length; r++) {
					if (sources[name].Rows[r].added) {
						r0++;
					}
				}
			}
			return r0;
		}
		_columnValueByIndex = function(name, ci, rowIndex) {
			if (sources && sources[name]) {
				if (typeof rowIndex == 'undefined') {
					rowIndex = sources[name].rowIndex;
				}
				else {
					rowIndex = parseInt(rowIndex);
				}
				if (rowIndex < sources[name].Rows.length && rowIndex >= 0 && ci >= 0 && ci < sources[name].Columns.length) {
					if (sources[name].Columns[ci].Type == 12) {
						if (sources[name].Rows[rowIndex].ItemArray[ci]) {
							if (typeof (sources[name].Rows[rowIndex].ItemArray[ci]) == 'string') {
								sources[name].Rows[rowIndex].ItemArray[ci] = JsonDataBinding.datetime.parseIso(sources[name].Rows[rowIndex].ItemArray[ci]);
							}
						}
						else {
							return new Date(0);
						}
					}
					else if (sources[name].Columns[ci].Type == 3
						|| sources[name].Columns[ci].Type == 1 || sources[name].Columns[ci].Type == 2
						|| sources[name].Columns[ci].Type == 8 || sources[name].Columns[ci].Type == 9
						|| sources[name].Columns[ci].Type == 16) {
						if (sources[name].Rows[rowIndex].ItemArray[ci]) {
							if (typeof (sources[name].Rows[rowIndex].ItemArray[ci]) == 'string') {
								sources[name].Rows[rowIndex].ItemArray[ci] = parseInt(sources[name].Rows[rowIndex].ItemArray[ci]);
							}
						}
						else {
							return 0;
						}
					}
					else if (sources[name].Columns[ci].Type == 11) {
						if (sources[name].Rows[rowIndex].ItemArray[ci]) {
							if (typeof (sources[name].Rows[rowIndex].ItemArray[ci]) == 'string') {
								var ts = new JsonDataBinding.timespan();
								ts.parseIsoString(sources[name].Rows[rowIndex].ItemArray[ci]);
								sources[name].Rows[rowIndex].ItemArray[ci] = ts;
							}
						}
						else {
							return new JsonDataBinding.timespan();
						}
					}
					else if (sources[name].Columns[ci].Type == 246 || sources[name].Columns[ci].Type == 0 || sources[name].Columns[ci].Type == 4 || sources[name].Columns[ci].Type == 5) {
						if (sources[name].Rows[rowIndex].ItemArray[ci]) {
							if (typeof (sources[name].Rows[rowIndex].ItemArray[ci]) == 'string') {
								sources[name].Rows[rowIndex].ItemArray[ci] = parseFloat(sources[name].Rows[rowIndex].ItemArray[ci]);
							}
						}
						else {
							return 0;
						}
					}
					return sources[name].Rows[rowIndex].ItemArray[ci];
				}
			}
			return null;
		}
		_columnValue = function(name, columnName, rowIndex) {
			var ci = _columnNameToIndex(name, columnName);
			return _columnValueByIndex(name, ci, rowIndex);
		}
		_isColumnValueNull = function(name, columnName, rowIndex) {
			if (sources && sources[name]) {
				if (typeof rowIndex == 'undefined') {
					rowIndex = sources[name].rowIndex;
				}
				if (rowIndex < sources[name].Rows.length && rowIndex >= 0) {
					var ci = _columnNameToIndex(name, columnName);
					if (typeof sources[name].Rows[rowIndex].ItemArray[ci] == 'undefined' ||
                              sources[name].Rows[rowIndex].ItemArray[ci] == null) {
						return true;
					}
					else {
						return false;
					}
				}
			}
			return true;
		}
		_isColumnValueNullOrEmpty = function(name, columnName, rowIndex) {
			if (sources && sources[name]) {
				if (typeof rowIndex == 'undefined') {
					rowIndex = sources[name].rowIndex;
				}
				if (rowIndex < sources[name].Rows.length && rowIndex >= 0) {
					var ci = _columnNameToIndex(name, columnName);
					if (typeof sources[name].Rows[rowIndex].ItemArray[ci] == 'undefined' ||
                              sources[name].Rows[rowIndex].ItemArray[ci] == null) {
						return true;
					}
					else {
						if (typeof sources[name].Rows[rowIndex].ItemArray[ci] == 'string') {
							return (sources[name].Rows[rowIndex].ItemArray[ci].length == 0);
						}
						else {
							return false;
						}
					}
				}
			}
			return true;
		}
		_isColumnValueNotNull = function(name, columnName, rowIndex) {
			if (sources && sources[name]) {
				if (typeof rowIndex == 'undefined') {
					rowIndex = sources[name].rowIndex;
				}
				if (rowIndex < sources[name].Rows.length && rowIndex >= 0) {
					var ci = _columnNameToIndex(name, columnName);
					if (typeof sources[name].Rows[rowIndex].ItemArray[ci] == 'undefined' ||
								sources[name].Rows[rowIndex].ItemArray[ci] == null) {
						return false;
					}
					else {
						return true;
					}
				}
			}
			return false;
		}
		_isColumnValueNotNullOrEmpty = function(name, columnName, rowIndex) {
			if (sources && sources[name]) {
				if (typeof rowIndex == 'undefined') {
					rowIndex = sources[name].rowIndex;
				}
				if (rowIndex < sources[name].Rows.length && rowIndex >= 0) {
					var ci = _columnNameToIndex(name, columnName);
					if (typeof sources[name].Rows[rowIndex].ItemArray[ci] == 'undefined' ||
							sources[name].Rows[rowIndex].ItemArray[ci] == null) {
						return false;
					}
					else {
						if (typeof sources[name].Rows[rowIndex].ItemArray[ci] == 'string') {
							return (sources[name].Rows[rowIndex].ItemArray[ci].length > 0);
						}
						else {
							return true;
						}
					}
				}
			}
			return false;
		}
		_isColumnValueChanged = function (name, columnName, rowIndex) {
			if (sources && sources[name]) {
				if (typeof rowIndex == 'undefined') {
					rowIndex = sources[name].rowIndex;
				}
				if (rowIndex < sources[name].Rows.length && rowIndex >= 0) {
					if (sources[name].Rows[rowIndex].ItemArray0) {
						var ci = _columnNameToIndex(name, columnName);
						if (ci >= 0 && ci < sources[name].Rows[rowIndex].ItemArray0.length) {
							return (sources[name].Rows[rowIndex].ItemArray0[ci] != sources[name].Rows[rowIndex].ItemArray[ci]);
						}
					}
				}
			}
			return false;
		}
		_setcolumnValue = function(name, columnName, val0, rowIndex) {
			if (sources && sources[name]) {
				if (typeof rowIndex == 'undefined') {
					rowIndex = sources[name].rowIndex;
				}
				if (rowIndex < sources[name].Rows.length && rowIndex >= 0) {
					var columnIndex = _columnNameToIndex(name, columnName);
					var val = val0;
					_backupdata(name, rowIndex);
					sources[name].Rows[rowIndex].ItemArray[columnIndex] = val;
					sources[name].Rows[rowIndex].changed = true;
					_onRowIndexChange(name);
					JsonDataBinding.onvaluechanged(sources[name], rowIndex, columnIndex, val);
				}
			}
		}
		//        _getcolumnValue = function(name, columnName, rowIndex) {
		//            if (sources && sources[name]) {
		//                if (typeof rowIndex == 'undefined') {
		//                    rowIndex = sources[name].rowIndex;
		//                }
		//                if (rowIndex < sources[name].Rows.length && rowIndex >= 0) {
		//                    var columnIndex = _columnNameToIndex(name, columnName);
		//                    return sources[name].Rows[rowIndex].ItemArray[columnIndex];
		//                }
		//            }
		//        }
		_getColExpvalue = function(name, expression, idx) {
			if (sources && sources[name]) {
				var exp = expression;
				for (var i = 0; i < sources[name].Columns.length; i++) {
					exp = exp.replace(new RegExp("{" + sources[name].Columns[i].Name + "}", "gi"), sources[name].Rows[idx].ItemArray[i]);
				}
				return eval(exp);
			}
			return null;
		}
		_columnExpressionValue = function(name, expression, rowIndex) {
			if (sources && sources[name]) {
				if (typeof rowIndex == 'undefined') {
					rowIndex = sources[name].rowIndex;
				}
				if (rowIndex < sources[name].Rows.length && rowIndex >= 0) {
					return _getColExpvalue(name, expression, rowIndex);
				}
			}
			return null;
		}
		_columnSum = function(name, fieldName) {
			return _statistics(name, fieldName, 'SUM');
		}
		//before modifying a column value this function is called to make a backup of the row so that the row can be rolled back if needed
		_backupdata = function (name, ridx) {
			if (sources && sources[name] && ridx >= 0 && sources[name].Rows && ridx < sources[name].Rows.length && sources[name].Rows[ridx]) {
				if (!sources[name].Rows[ridx].ItemArray0) {
					sources[name].Rows[ridx].ItemArray0 = sources[name].Rows[ridx].ItemArray;
					sources[name].Rows[ridx].ItemArray = new Array();
					for (var c = 0; c < sources[name].Rows[ridx].ItemArray0.length; c++) {
						sources[name].Rows[ridx].ItemArray[c] = sources[name].Rows[ridx].ItemArray0[c];
					}
				}
			}
		}
		_refreshTableDisplay = function (e, dataName, rowIdx) {
			if (e && e.children) {
				for (var i = 0; i < e.children.length; i++) {
					if (e.children[i].jsData && e.children[i].jsData.refreshBindColumnDisplay) {
						e.children[i].jsData.refreshBindColumnDisplay(dataName, rowIdx);
					}
					else {
						_refreshTableDisplay(e.children[i], dataName, rowIdx);
					}
				}
			}
		}
		_cancelDataEdit = function (name) {
			if (sources && sources[name] && sources[name].Rows) {
				var curRowIndex;
				if (typeof sources[name].rowIndex == 'undefined')
					curRowIndex = sources[name].rowIndex;
				//var restored = false;
				for (var i = 0; i < sources[name].Rows.length; i++) {
					if (sources[name].Rows[i].ItemArray0) {
						sources[name].Rows[i].ItemArray = sources[name].Rows[i].ItemArray0;
						sources[name].Rows[i].changed = false;
						delete sources[name].Rows[i].ItemArray0;
						if (typeof sources[name].Rows[i].rowVersion == 'undefined')
							sources[name].Rows[i].rowVersion = 1;
						else
							sources[name].Rows[i].rowVersion = sources[name].Rows[i].rowVersion + 1;
						if (sources[name].rowIndex == i) {
							sources[name].rowIndex = i;
							bindData(document.body, name);
						}
						else {
							_refreshTableDisplay(document.body, name, i);
						}
						//restored = true;
					}
				}
				//if (restored) {
				//	if (typeof curRowIndex != 'undefined' && sources[name].rowIndex != curRowIndex) {
				//		sources[name].rowIndex = curRowIndex;
				//	}
				//}
			}
		}
		_statistics = function(name, expression, operator) {
			if (sources && sources[name]) {
				if (typeof rowIndex == 'undefined') {
					rowIndex = sources[name].rowIndex;
				}
				var sum = 0.0, idx, i, m, v;
				if (operator == 'SUM') {
					for (idx = 0; idx < sources[name].Rows.length; idx++) {
						sum = sum + _getColExpvalue(name, expression, idx);
					}
					return sum;
				}
				else if (operator == "AVG") {
					if (sources[name].Rows.length > 0) {
						for (idx = 0; idx < sources[name].Rows.length; idx++) {
							var exp = expression;
							for (i = 0; i < sources[name].columnIndexes.length; i++) {
								exp = exp.replace(new RegExp("{" + sources[name].Columns[i].Name + "}", "gi"), sources[name].Rows[idx].ItemArray[_columnNameToIndex(name, sources[name].Columns[i].Name)]);
							}
							sum = sum + _getColExpvalue(name, expression, idx);
						}
						return sum / sources[name].Rows.length;
					}
					return sum;
				}
				else if (operator == "MIN") {
					if (sources[name].Rows.length > 0) {
						idx = 0;
						m = _getColExpvalue(name, expression, idx);
						for (idx = 1; idx < sources[name].Rows.length; idx++) {
							v = _getColExpvalue(name, expression, idx);
							if (v < m) {
								m = v;
							}
						}
						return m;
					}
				}
				else if (operator == "MAX") {
					if (sources[name].Rows.length > 0) {
						idx = 0;
						m = _getColExpvalue(name, expression, idx);
						for (idx = 1; idx < sources[name].Rows.length; idx++) {
							v = _getColExpvalue(name, expression, idx);
							if (v > m) {
								m = v;
							}
						}
						return m;
					}
				}
			}
			return null;
		}
		_toNumber = function(v) {
			if (typeof v == 'undefined' || v == null)
				return 0;
			return v;
		}
		_sortOnColumn = function(name, columnName, sortAsc, ignoreCase) {
			if (sources && sources[name] && sources[name].Rows.length > 0) {
				var columnIndex = _columnNameToIndex(name, columnName);
				if (sources[name].Columns[columnIndex].Type == 16 /*|| sources[name].Columns[columnIndex].Type == 1*/) {
					//BIT
					sources[name].Rows.sort(function(a, b) {
						var a0 = _toNumber(a.ItemArray[columnIndex]);
						var b0 = _toNumber(b.ItemArray[columnIndex]);
						if (a0 < b0) {
							if (sortAsc)
								return -1;
							else
								return 1;
						}
						else if (a0 > b0) {
							if (sortAsc)
								return 1;
							else
								return -1;
						}
						return 0;
					});
				}
				else if (sources[name].Columns[columnIndex].Type < 10 || sources[name].Columns[columnIndex].Type == 246) {
					//number
					sources[name].Rows.sort(function(a, b) {
						var a0 = _toNumber(a.ItemArray[columnIndex]);
						var b0 = _toNumber(b.ItemArray[columnIndex]);
						if (sortAsc)
							return a0 - b0;
						else
							return b0 - a0;
					});
				}
				else if (sources[name].Columns[columnIndex].Type < 16) {
					//datetime
					sources[name].Rows.sort(function(a, b) {
						var a0 = JsonDataBinding.datetime.toDate0(a.ItemArray[columnIndex]);
						var b0 = JsonDataBinding.datetime.toDate0(b.ItemArray[columnIndex]);
						if (sortAsc)
							return a0 - b0;
						else
							return b0 - a0;
					});
				}
				else {
					//string
					sources[name].Rows.sort(function(a, b) {
						var a0 = (typeof a.ItemArray[columnIndex] == 'undefined' || a.ItemArray[columnIndex] == null) ? '' : a.ItemArray[columnIndex];
						var b0 = (typeof b.ItemArray[columnIndex] == 'undefined' || b.ItemArray[columnIndex] == null) ? '' : b.ItemArray[columnIndex];
						if (ignoreCase) {
							a0 = a0.toLowerCase();
							b0 = b0.toLowerCase();
						}
						if (a0 == b0)
							return 0;
						if (sortAsc) {
							if (a0 > b0)
								return 1;
							else
								return -1;
							//return (a0 > b0);//a0.localeCompare(b0);
						}
						else {
							if (b0 > a0)
								return 1;
							else
								return -1;
							//return (b0 > a0);//b0.localeCompare(a0);
						}
					});
				}
				bindTable(name, true, false);
			}
		}
		_deleteCurrentRow = function(name) {
			if (sources && sources[name]) {
				var idx = sources[name].rowIndex;
				if (idx >= 0 && idx < sources[name].Rows.length) {
					preserveKeys(name);
					sources[name].Rows[idx].deleted = true;
					var idx2 = getNextRowIndex(name, idx);
					if (idx2 < 0) {
						idx2 = getPreviousRowIndex(name, idx);
					}
					if (typeof onrowdeletehandlers[name] != 'undefined') {
						for (var i = 0; i < onrowdeletehandlers[name].length; i++) {
							onrowdeletehandlers[name][i](name, idx);
						}
					}
					sources[name].rowIndex = idx2;
					bindData(document.body, name, false);
					return true;
				}
			}
			return false;
		}
		_getCurrentRowIndex = function(name) {
			if (sources && sources[name]) {
				return sources[name].rowIndex;
			}
			return -1;
		}
		_getRowCount = function(name) {
			if (sources && sources[name]) {
				return sources[name].Rows.length;
			}
			return 0;
		}
		_addRow = function(name) {
			if (sources && sources[name]) {
				var r = new Object();
				r.added = true;
				r.ItemArray = new Array();
				for (var i = 0; i < sources[name].Columns.length; i++) {
					if (sources[name].Columns[i].isAutoNumber) {
						r.ItemArray[i] = -Math.floor(Math.random() * 1000000);
					}
					else {
						r.ItemArray[i] = null;
					}
				}
				var idx = sources[name].Rows.length;
				sources[name].Rows[idx] = r;
				sources[name].rowIndex = idx;
				bindData(document.body, name, false);
				return idx;
			}
			return -1;
		}
		_resetDataStreaming = function(name) {
			_setTableAttribute(name, 'isDataStreaming', false);
		}
		_saveEdsToOffline = function (name, dataName) {
			if (window.localStorage) {
				if (sources[name]) {
					var n = 'limnoreds_' + dataName;
					var vstr = JSON.stringify(sources[name]);
					window.localStorage[n] = vstr;
				}
			}
		}
		_loadEdsFromOffline = function (name, dataName) {
			if (window.localStorage) {
				var n = 'limnoreds_' + dataName;
				var vstr = window.localStorage[n];
				if (typeof vstr != 'undefined') {
					var data = JSON.parse(vstr);
					sources[name] = data;
					_bindData(document.body, name, true, false);
				}
			}
		}
		_removeEdsFromOffline = function (dataName) {
			if (window.localStorage) {
				var n = 'limnoreds_' + dataName;
				if (typeof window.localStorage[n] != 'undefined') {
					delete window.localStorage[n];
				}
			}
		}
		//_setDataStreaming = function (name) {
		//    _setTableAttribute(name, 'isDataStreaming', true);
		//}
		function preserveKeys(name) {
			var tbl = sources[name];
			if (tbl.rowIndex >= 0 && tbl.rowIndex < tbl.Rows.length) {
				if (tbl.PrimaryKey != null && tbl.PrimaryKey.length > 0) {
					if (!tbl.Rows[tbl.rowIndex].changed && !tbl.Rows[tbl.rowIndex].deleted && !tbl.Rows[tbl.rowIndex].removed) {
						if (typeof tbl.Rows[tbl.rowIndex].KeyValues == 'undefined') {
							tbl.Rows[tbl.rowIndex].KeyValues = new Array();
							for (var k = 0; k < tbl.PrimaryKey.length; k++) {
								var ci = _columnNameToIndex(tbl.TableName, tbl.PrimaryKey[k]);
								tbl.Rows[tbl.rowIndex].KeyValues[k] = tbl.Rows[tbl.rowIndex].ItemArray[ci];
							}
						}
					}
				}
			}
		}
		//onchange occured
		//supported by fileUpload, select, text, textarea
		function changeBoundData(e) {
			var a;
			var rIdx;
			var rIdxs;
			if (e && typeof e.jsdbRowIndex != 'undefined') {
				rIdxs = e.jsdbRowIndex;
			}
			if (e && typeof e.onsetbounddata == 'function') {
				a = e;
			}
			if (!a) {
				a = getEventSender(e);
				if (!a) {
					a = e;
				}
			}
			if (a) {
				if (typeof rIdx == 'undefined') {
					if (typeof a.jsdbRowIndex != 'undefined') {
						rIdxs = a.jsdbRowIndex;
					}
				}
				//
				var dbs = a.getAttribute(jsdb_bind);
				if (typeof dbs != 'undefined' && dbs != null && dbs != '') {
					var binds = dbs.split(';');
					for (var sIdx = 0; sIdx < binds.length; sIdx++) {
						var bind = binds[sIdx].split(':');
						var sourceName = bind[0];
						var tbl = sources[sourceName];
						if (typeof tbl != 'undefined') {
							var field = bind[1];
							var target = bind[2];
							if (target == 'checked' || target == 'value' || target == 'innerHTML' || target == 'innerText') {
								if (rIdxs) {
									rIdx = rIdxs[sourceName];
								}
								//missing a link between an event and the bound property
								//for example, onchange is for value
								//maybe use a constant mapping
								var rIdx0 = tbl.rowIndex;
								if (typeof rIdx == 'undefined') {
									rIdx = rIdx0;
								}
								if (rIdx >= 0 && rIdx < tbl.Rows.length) {
									_backupdata(tbl.Tablename, rIdx);
									tbl.rowIndex = rIdx;
									preserveKeys(sourceName);
									var c = _columnNameToIndex(tbl.TableName, field);
									var v;
									if (target == 'innerText') {
										v = JsonDataBinding.GetInnerText(a);
									}
									else {
										v = a[target];
									}
									tbl.Rows[rIdx].ItemArray[c] = v;
									tbl.Rows[rIdx].changed = true;
									JsonDataBinding.onvaluechanged(tbl, rIdx, c, v);
									tbl.rowIndex = rIdx0;
								}
								//break;
							}
						}
					}
				}
			}
		}
		_ondataupdatefailed = function (name) {
			_executeEventHandlers('DataUpdateFailed', name);
		}
		_ondataupdated = function(name) {
			for (p in sources) {
				var item = sources[p];
				if (typeof item != 'undefined' && item != null && typeof (item) != type_func) {
					if (typeof item.TableName == 'undefined') {
						continue;
					}
					if (typeof name != 'undefined') {
						if (name != item.TableName) {
							continue;
						}
					}
					var rows = item.Rows;
					for (var i = 0; i < rows.length; i++) {
						if (rows[i].added) {
							delete rows[i].added;
						}
						if (rows[i].changed) {
							delete rows[i].changed;
						}
						if (rows[i].deleted) {
							delete rows[i].deleted;
							rows[i].removed = true;
						}
						if (rows[i].ItemArray0) {
							delete rows[i].ItemArray0;
						}
					}
					_executeEventHandlers('DataUpdated', name);
				}
			}
		}
		_collectModifiedData = function (dataName, polled) {
			var item = sources[dataName];
			if (item) {
				if (!polled) {
					JsonDataBinding.pollModifications();
				}
				var modifiedData = new Object();
				var hasDate = false;
				var utc = _getTableAttribute(dataName, 'AutoConvertUTCforWeb');
				//do not send image data over the internet
				var hasImage = false;
				var imageFlags = _getObjectProperty(dataName, 'IsFieldImage');
				var nDim = 0;
				if (imageFlags && imageFlags.length > 0) {
					nDim = Math.min(imageFlags.length, item.Columns.length);
					for (c0 = 0; c0 < nDim; c0++) {
						if (imageFlags[c0] && item.Columns[c0].Type == 252) {
							hasImage = true;
							item.Columns[c0].ReadOnly = true;
						}
					}
				}
				for (c0 = 0; c0 < item.Columns.length; c0++) {
					if (item.Columns[c0].Type == 11 || item.Columns[c0].Type == 12) {
						hasDate = true;
						break;
					}
				}
				for (var n in item) {
					var n0 = item[n];
					if (typeof n0 != 'undefined' && n0 != null && typeof (n0) != type_func) {
						if (n == 'Rows') {
							var rs = n0;
							var rs2 = new Array();
							var k = 0;
							for (var j = 0; j < rs.length; j++) {
								if (rs[j].changed || rs[j].deleted || rs[j].added) {
									if (!(rs[j].deleted && rs[j].added)) {
										if (rs[j].ItemArray0) {
											delete rs[j].ItemArray0;
										}
										if (hasImage || hasDate) {
											//reconstruct the row
											var rowBuf = {};
											for (var nr in rs[j]) {
												var nr0 = rs[j][nr];
												if (typeof nr0 != 'undefined' && nr0 != null && typeof (nr0) != type_func) {
													if (nr == 'ItemArray') {
														rowBuf[nr] = new Array();
														for (var c0 = 0; c0 < nr0.length; c0++) {
															if (c0 < nDim && imageFlags[c0] && item.Columns[c0].Type == 252) {
																rowBuf[nr].push('');
															}
															else {
																if (typeof (nr0[c0]) == 'undefined' || nr0[c0] == null) {
																	rowBuf[nr].push(null);
																}
																else if (item.Columns[c0].Type == 12) {
																	if (utc) {
																		rowBuf[nr].push(JsonDataBinding.datetime.toIsoUTC(nr0[c0]));
																	}
																	else {
																		rowBuf[nr].push(JsonDataBinding.datetime.toIso(nr0[c0]));
																	}
																}
																else if (item.Columns[c0].Type == 11) {
																	if (typeof (nr0[c0].toTimeString) == type_func)
																		rowBuf[nr].push(nr0[c0].toTimeString());
																	else
																		rowBuf[nr].push(nr0[c0]);
																}
																else {
																	rowBuf[nr].push(nr0[c0]);
																}
															}
														}
													}
													else {
														rowBuf[nr] = nr0;
													}
												}
											}
											rs2[k++] = rowBuf;
										}
										else {
											rs2[k++] = rs[j];
										}
									}
								}
							}
							modifiedData[n] = rs2;
						}
						else {
							modifiedData[n] = n0;
						}
					}
				}
				return modifiedData;
			}
		}
		_sendBoundData = function(dataName, clientProperties, commands) {
			JsonDataBinding.pollModifications();
			var req = new Object();
			req.Calls = new Array();
			if (typeof commands != 'undefined') {
				req.Calls = commands;
			}
			if (typeof clientProperties != 'undefined' && clientProperties != null) {
				req.values = clientProperties;
			}
			req.Data = new Array();
			var i = 0;
			var c0;
			for (var p in sources) {
				var item = sources[p];
				if (typeof item != 'undefined' && item != null && typeof (item) != type_func) {
					if (typeof dataName != 'undefined' && dataName != '' && dataName != null) {
						if (item.TableName != dataName) {
							continue;
						}
					}
					var modifiedData = _collectModifiedData(item.TableName, true);
					if (modifiedData) {
						req.Data[i] = modifiedData;
					}
					//var hasDate = false;
					//var utc = _getTableAttribute(dataName, 'AutoConvertUTCforWeb');
					////do not send image data over the internet
					//var hasImage = false;
					//var imageFlags = _getObjectProperty(dataName, 'IsFieldImage');
					//var nDim = 0;
					//if (imageFlags && imageFlags.length > 0) {
					//	nDim = Math.min(imageFlags.length, item.Columns.length);
					//	for (c0 = 0; c0 < nDim; c0++) {
					//		if (imageFlags[c0] && item.Columns[c0].Type == 252) {
					//			hasImage = true;
					//			item.Columns[c0].ReadOnly = true;
					//		}
					//	}
					//}
					//for (c0 = 0; c0 < item.Columns.length; c0++) {
					//	if (item.Columns[c0].Type == 11 || item.Columns[c0].Type == 12) {
					//		hasDate = true;
					//		break;
					//	}
					//}
					////
					//req.Data[i] = new Object(); //a new table
					//for (var n in item) {
					//	var n0 = item[n];
					//	if (typeof n0 != 'undefined' && n0 != null && typeof (n0) != type_func) {
					//		if (n == 'Rows') {
					//			var rs = n0;
					//			var rs2 = new Array();
					//			var k = 0;
					//			for (var j = 0; j < rs.length; j++) {
					//				if (rs[j].changed || rs[j].deleted || rs[j].added) {
					//					if (!(rs[j].deleted && rs[j].added)) {
					//						if (hasImage || hasDate) {
					//							//reconstruct the row
					//							var rowBuf = {};
					//							for (nr in rs[j]) {
					//								var nr0 = rs[j][nr];
					//								if (typeof nr0 != 'undefined' && nr0 != null && typeof (nr0) != type_func) {
					//									if (nr == 'ItemArray') {
					//										rowBuf[nr] = new Array();
					//										for (c0 = 0; c0 < nr0.length; c0++) {
					//											if (c0 < nDim && imageFlags[c0] && item.Columns[c0].Type == 252) {
					//												rowBuf[nr].push('');
					//											}
					//											else {
					//												if (typeof (nr0[c0]) == 'undefined' || nr0[c0] == null) {
					//													rowBuf[nr].push(null);
					//												}
					//												else if (item.Columns[c0].Type == 12) {
					//													if (utc) {
					//														rowBuf[nr].push(JsonDataBinding.datetime.toIsoUTC(nr0[c0]));
					//													}
					//													else {
					//														rowBuf[nr].push(JsonDataBinding.datetime.toIso(nr0[c0]));
					//													}
					//												}
					//												else if (item.Columns[c0].Type == 11) {
					//													if (typeof (nr0[c0].toTimeString) == type_func)
					//														rowBuf[nr].push(nr0[c0].toTimeString());
					//													else
					//														rowBuf[nr].push(nr0[c0]);
					//												}
					//												else {
					//													rowBuf[nr].push(nr0[c0]);
					//												}
					//											}
					//										}
					//									}
					//									else {
					//										rowBuf[nr] = nr0;
					//									}
					//								}
					//							}
					//							rs2[k++] = rowBuf;
					//						}
					//						else {
					//							rs2[k++] = rs[j];
					//						}
					//					}
					//				}
					//			}
					//			req.Data[i][n] = rs2;
					//		}
					//		else {
					//			req.Data[i][n] = n0;
					//		}
					//	}
					//}
					i++;
				}
			}
			_callServer(req);
		}
		_submitBoundData = function() {
			_sendBoundData('', null, [{ method: jsdb_putdata, value: '0'}]);
		}
		_putData = function(dataName) {
			_sendBoundData(dataName, null, [{ method: jsdb_putdata, value: dataName}]);
		}
		_mergeValues = function(vs) {
			var obj = {};
			for (var n in vs) {
				if (vs.hasOwnProperty(n)) {
					JsonDataBinding.values[n] = vs[n];
					var name;
					if (JsonDataBinding.startsWith(n, 'autoNumList_')) {
						name = n.substr(12);
						var kvs = vs[n];
						var tbl = sources[name];
						if (tbl && tbl.Rows && kvs && kvs.length > 0) {
							var ai = -1;
							for (j = 0; j < tbl.Columns.length; j++) {
								if (tbl.Columns[j].isAutoNumber) {
									ai = j;
									break;
								}
							}
							if (ai >= 0) {
								var kl = kvs.length;
								var k0 = 0;
								for (var r = 0; r < tbl.Rows.length; r++) {
									for (var r0 = 0; r0 < kl; r0++) {
										if (kvs[r0].key == tbl.Rows[r].ItemArray[ai]) {
											tbl.Rows[r].ItemArray[ai] = kvs[r0].value;
											refreshBindColumnDisplay(name, r, ai);
											k0++;
											break;
										}
									}
									if (k0 >= kl) {
										break;
									}
								}
							}
						}
					}
					else if (JsonDataBinding.startsWith(n, 'batchSreamID_')) {
						name = n.substr(13);
						if (!obj[name]) {
							obj[name] = {};
						}
						obj[name].streamId = vs[n];
					}
					else if (JsonDataBinding.startsWith(n, 'batchFunction_')) {
						name = n.substr(14);
						if (!obj[name]) {
							obj[name] = {};
						}
						obj[name].functionName = vs[n];
					}
					else if (JsonDataBinding.startsWith(n, 'batchKey_')) {
						name = n.substr(9);
						if (!obj[name]) {
							obj[name] = {};
						}
						obj[name].batchKey = vs[n];
					}
					else if (JsonDataBinding.startsWith(n, 'batchIsFirst_')) {
						name = n.substr(13);
						if (!obj[name]) {
							obj[name] = {};
						}
						obj[name].isFirstBatch = vs[n];
					}
					else if (JsonDataBinding.startsWith(n, 'batchObjName_')) {
						name = n.substr(13);
						if (!obj[name]) {
							obj[name] = {};
						}
						obj[name].serverComponentName = vs[n];
					}
					else if (JsonDataBinding.startsWith(n, 'batchParameter_')) {
						var name2 = n.substr(15);
						var pos = name2.indexOf('_');
						if (pos > 0) {
							var pa = name2.substr(0, pos);
							name = name2.substr(pos + 1);
							if (!obj[name]) {
								obj[name] = {};
							}
							var ob = obj[name];
							if (!ob.parameters) {
								ob.parameters = {};
							}
							ob.parameters[pa] = vs[n];
						}
					}
					else if (JsonDataBinding.startsWith(n, 'batchWhere_')) {
						name = n.substr(11);
						if (!obj[name]) {
							obj[name] = {};
						}
						obj[name].batchWhere = vs[n];
					}
					else if (JsonDataBinding.startsWith(n, 'batchWhereParams_')) {
						name = n.substr(17);
						if (!obj[name]) {
							obj[name] = {};
						}
						obj[name].batchWhereParams = vs[n];
					}
					else if (JsonDataBinding.startsWith(n, 'uploadedValues_')) {
						name = n.substr(15);
						if (!obj[name]) {
							obj[name] = {};
						}
						obj[name].uploadedValues = vs[n];
					}
					else if (JsonDataBinding.startsWith(n, 'masterfield_')) {
						name2 = n.substr(12);
						var pos = name2.indexOf('_');
						if (pos > 0) {
							var pa = name2.substr(0, pos);
							name = name2.substr(pos + 1);
							if (!obj[pa]) {
								obj[pa] = {};
							}
							var ob = obj[pa];
							if (!ob.fields) {
								ob.fields = {};
							}
							ob.fields[name] = vs[n];
						}
					}
				}
			}
			return obj;
		}
		function _getFormClientHolder(form) {
			if (form) {
				function getfc(f) {
					for (var i = 0; i < f.children.length; i++) {
						if (f.children[i].getAttribute('name') == 'clientRequest') {
							return f.children[i];
						}
						else {
							var e = getfc(f.children[i]);
							if (e)
								return e;
						}
					}
				}
				var d = getfc(form);
				if (d)
					return d;
				//var idCust = form.getAttribute('clientRequest');
				//if (idCust) {
				//	return document.getElementById(idCust);
				//}
				//else {

				//}
			}
			return document.clientRequest;
		}
		function _getFormMaxSizeHolder(form) {
			if (form) {
				function getfc(f) {
					for (var i = 0; i < f.children.length; i++) {
						if (f.children[i].getAttribute('name') == 'MAX_FILE_SIZE') {
							return f.children[i];
						}
						else {
							var e = getfc(f.children[i]);
							if (e)
								return e;
						}
					}
				}
				return getfc(form);
			}
		}
		var DEBUG_SYMBOL = "F3E767376E6546a8A15D97951C849CE5";
		_processServerResponse = function(r, state, reportError) {
			var v, winDebug;
			var raw0 = r;
			var pos = r.indexOf(DEBUG_SYMBOL);
			if (pos >= 0 || JsonDataBinding.Debug || reportError) {
				var debug;
				if (pos >= 0) {
					debug = r.substring(0, pos);
					r = r.substring(pos + DEBUG_SYMBOL.length);
				}
				else {
					debug = r;
				}
				winDebug = JsonDataBinding.OpenDebugWindow(); //window.open(null, "debugWindows",null,true);
				if (winDebug == null) {
					alert('Debug information cannot be displayed. Your web browser has disabled pop-up window');
				}
				else {
					winDebug.document.write('<h1>Debug Information from ');
					winDebug.document.write(window.location.pathname);
					winDebug.document.write('</h1>');
					winDebug.document.write('<h2>Client request</h2><p>');
					winDebug.document.write('Client page:');
					winDebug.document.write(window.location.href);
					winDebug.document.write('<br>');
					winDebug.document.write(debug);
					winDebug.document.write('</p>');
					winDebug.document.write('<h2>Server response</h2><p>');
					winDebug.document.write('Server page:');
					if (state && state.serverPage) {
						winDebug.document.write(state.serverPage);
					}
					winDebug.document.write('<br>');
					winDebug.document.write(r);
					winDebug.document.write('</p>');
				}
			}
			if (typeof r != 'undefined' && r != null && r.length > 6) {
				var signCC = r.substr(0, 6).toLowerCase();
				if (signCC == '<html>') {
					winDebug = JsonDataBinding.OpenDebugWindow();
					if (winDebug == null) {
						alert('Debug information cannot be displayed. Your web browser has disabled pop-up window');
					}
					else {
						winDebug.document.documentElement.innerHTML = r;
					}
				}
				else {
					for (var k = 0; k < r.length; k++) {
						//remove 65279 
						if (r.charAt(k) == '{') {
							r = r.substring(k);
							break;
						}
					}
					pos = r.length - 1;
					while (r.charAt(pos) != '}') {
						pos--;
						if (pos <= 0) {
							r = '{}';
							break;
						}
					}
					if (pos > 0 && pos < r.length - 1) {
						r = r.substr(0, pos + 1);
					}
					try {
						v = JSON.parse(r); // r.parseJSON();
					}
					catch (err) {
						winDebug = JsonDataBinding.OpenDebugWindow(); //window.open("", "debugWindows");
						if (winDebug == null) {
							alert('Debug information cannot be displayed. Your web browser has disabled pop-up window');
						}
						else {
							winDebug.document.write('<h1>Exception Information from ');
							winDebug.document.write(window.location.pathname);
							//winDebug.document.write(jsdb_serverPage);
							winDebug.document.write('</h1>');
							winDebug.document.write('Client page:');
							winDebug.document.write(window.location.href);
							winDebug.document.write('<br>');
							winDebug.document.write('Server page:');
							if (state && state.serverPage) {
								winDebug.document.write(state.serverPage);
							}
							winDebug.document.write('<br>');
							if (pos < 0) {
								winDebug.document.write('<h2>Client request</h2><p>');
								if (state && state.Data) {
									winDebug.document.write(JSON.stringify(state.Data)); //.toJSONString());
								}
								else {
									var formCust = _getFormClientHolder(JsonDataBinding.SubmittedForm);
									if (formCust && formCust.value) {
										pos = formCust.value.indexOf(DEBUG_SYMBOL);
										var data;
										if (pos >= 0) {
											//var debug = r.substring(0, pos);
											data = formCust.value.substring(pos + DEBUG_SYMBOL.length);
										}
										else {
											data = formCust.value;
										}
										winDebug.document.write(data);
									}
								}
								winDebug.document.write('</p>');
								winDebug.document.write('<h2>Server response</h2><p>');
								winDebug.document.write(r);
								winDebug.document.write('</p>');
							}
							winDebug.document.write('<h2>Json exception</h2><p>');
							winDebug.document.write('<table>');
							for (var p in err) {
								winDebug.document.write('<tr><td>');
								winDebug.document.write(p);
								winDebug.document.write('</td><td>');
								winDebug.document.write(err[p]);
								winDebug.document.write('</td></tr>');
							}
							winDebug.document.write('</table>');
							winDebug.document.write('</p>');
							winDebug.document.write('Server response:<br><textarea readonly  rows="30" style="width:90%; ">');
							winDebug.document.write(raw0);
							winDebug.document.write('</textarea><br>Json data:<br><textarea readonly  rows="30" style="width:90%; ">');
							winDebug.document.write(r);
							winDebug.document.write('</textarea>');
						}
					}
					if (v) {
						var dataAttrs = _mergeValues(v.values);
						_serverComponentName = v.serverComponentName;
						var addednewrecord = JsonDataBinding.values.addednewrecord;
						var updatedtables = JsonDataBinding.values.updatedtables;
						var updatefailedtables = JsonDataBinding.values.updatefailedtables;
						var serverFailure = JsonDataBinding.values.serverFailure;
						if (typeof JsonDataBinding.values.addednewrecord != 'undefined') {
							delete JsonDataBinding.values.addednewrecord;
						}
						if (typeof JsonDataBinding.values.serverFailure != 'undefined') {
							delete JsonDataBinding.values.serverFailure;
						}
						if (typeof JsonDataBinding.values.updatedtables != 'undefined') {
							delete JsonDataBinding.values.updatedtables;
						}
						if (typeof JsonDataBinding.values.updatefailedtables != 'undefined') {
							delete JsonDataBinding.values.updatefailedtables;
						}
						if (JsonDataBinding.SubmittedForm) {
							if (JsonDataBinding.values.SavedFiles) {
								JsonDataBinding.SubmittedForm.SavedFilePaths = JsonDataBinding.values.SavedFiles;
							}
						}
						if (updatedtables) {
							for (var i = 0; i < updatedtables.length; i++) {
								JsonDataBinding.setTableAttribute(updatedtables[i], '');
							}
						}
						if (updatefailedtables) {
							for (var i = 0; i < updatefailedtables.length; i++) {
								JsonDataBinding.setTableAttribute(updatefailedtables[i].key, 'ErrorMessage', updatefailedtables[i].value);
							}
						}
						//if (typeof state != 'undefined' && state.cursor) {
						//	document.body.style.cursor = state.cursor;
						//}
						//else {
						//	document.body.style.cursor = 'default';
						//}
						if (typeof v.values != 'undefined' && v.values != null && limnorPage && limnorPage.customprops) {
							for (var i = 0; i < limnorPage.customprops.length; i++) {
								if (typeof (v.values[limnorPage.customprops[i].code]) != 'undefined') {
									JsonDataBinding.PageValues[limnorPage.customprops[i].name] = v.values[limnorPage.customprops[i].code];
								}
							}
						}
						if (typeof v.Data != 'undefined' && v.Data != null) {
							_setDataSource.call(v.Data, dataAttrs); //v.Data is a JsonDataSet
						}
						if (typeof v.Calls != 'undefined' && v.Calls.length > 0) {
							var cf = function() {
								for (var i = 0; i < v.Calls.length; i++) {
									try {
										eval(v.Calls[i]);
									}
									catch (eeval) {
										winDebug = JsonDataBinding.OpenDebugWindow(); //window.open("", "debugWindows");
										if (winDebug == null) {
											alert('Debug information cannot be displayed. Your web browser has disabled pop-up window');
										}
										else {
											winDebug.document.write("Error executing script after server connect:<br><code>");
											winDebug.document.write(v.Calls[i]);
											winDebug.document.write("</code><br>");
										}
									}
								}
							}
							cf.call(v);
						}
						if (typeof JsonDataBinding == 'undefined') return;
						JsonDataBinding.values.isdatastreaming = null;
						_executeClientEventObject('onProcessServerCall');
						_executeClientEventObject('ExecuteFinish');
						_executeClientEventObject('FinishedDataTransfer');
						if (addednewrecord && addednewrecord.length > 0) {
							//each item is a table name
							for (var i = 0; i < addednewrecord.length; i++) {
								_executeEventHandlers('DataUpdated', addednewrecord[i], true);
							}
						}
						if (updatedtables && updatedtables.length > 0) {
							for (var i = 0; i < updatedtables.length; i++) {
								//each item is a table name
								_ondataupdated(updatedtables[i]);
							}
						}
						//
						if (updatefailedtables && updatefailedtables.length > 0) {
							for (var i = 0; i < updatefailedtables.length; i++) {
								//each item is a key/value pair, key is a table name, value is error message
								_ondataupdatefailed(updatefailedtables[i].key);
							}
						}
						if (_clientEventsHolder && serverFailure) {
							var eh = _clientEventsHolder['onwebserverreturn'];
							if (eh) {
								for (var cname in eh) {
									var eho = eh[cname];
									if (eho && eho.handlers && eho.handlers.length > 0) {
										for (var i = 0; i < eho.handlers.length; i++) {
											eho.handlers[i](serverFailure);
										}
									}
								}
							}
						}
					}
				}
			}
			if (!JsonDataBinding.AbortEvent && state && state.Data && state.Data.values && state.Data.values.nextBlock) {
				state.Data.values.nextBlock();
			}
			if (typeof state != 'undefined' && typeof state.JsEventOwner != 'undefined' && state.JsEventOwner != null) {
				if (typeof state.JsEventOwner.disabled != 'undefined') {
					state.JsEventOwner.disabled = false;
				}
			}
			else {
				if (typeof _eventFirer != 'undefined' && _eventFirer != null) {
					if (typeof _eventFirer.disabled != 'undefined') {
						_eventFirer.disabled = false;
						_eventFirer = null;
					}
				}
			}
			//if (typeof state != 'undefined' && state.cursor) {
			//	document.body.style.cursor = state.cursor;
			//}
			//else {
			//	document.body.style.cursor = 'default';
			//}
			if (JsonDataBinding.ShowAjaxCallWaitingImage) {
				JsonDataBinding.ShowAjaxCallWaitingImage.style.display = 'none';
			}
			if (JsonDataBinding.ShowAjaxCallWaitingLabel) {
				JsonDataBinding.ShowAjaxCallWaitingLabel.style.display = 'none';
			}
		}
		//Ajax
		_callServer = function(data, form, execAttrs) {
			if (JsonDataBinding.LogonPage.length > 0) {
				if (JsonDataBinding.hasLoggedOn() != 2) {
					var curUrl = JsonDataBinding.getPageFilename();
					window.location.href = JsonDataBinding.LogonPage + '?' + curUrl;
					return;
				}
			}
			var state = {};
			//if (!(execAttrs && execAttrs.background)) {
			//	if (document.body.style.cursor != 'wait') {
			//		state.cursor = document.body.style.cursor;
			//		document.body.style.cursor = 'wait';
			//	}
			//}
			if (JsonDataBinding.ShowAjaxCallWaitingImage) {
				JsonDataBinding.ShowAjaxCallWaitingImage.style.display = '';
			}
			if (JsonDataBinding.ShowAjaxCallWaitingLabel) {
				JsonDataBinding.ShowAjaxCallWaitingLabel.style.display = '';
			}
			state.Data = data;
			if (typeof _eventFirer != 'undefined' && _eventFirer != null) {
				if (typeof _eventFirer.disabled != 'undefined') {
					_eventFirer.disabled = true;
					state.JsEventOwner = _eventFirer;
				}
			}
			JsonDataBinding.pageMoveout = false;
			if (data.values) {
				for (var nm in data.values) {
					if (data.values[nm] && typeof (data.values[nm].toTimeString) == 'function') {
						data.values[nm] = data.values[nm].toTimeString();
					}
				}
			}
			if (form) {
				if (form.submit) {
					var sizeInput = _getFormMaxSizeHolder(form);
					if (sizeInput) {
						var msize = sizeInput.getAttribute('value');
						if (typeof msize != 'undefined' && msize != null && msize > 0) {
							if (!data.values)
								data.values = {};
							data.values.allowedFileSize = msize;
						}
					}
					if (typeof JsonDataBinding.Debug != 'undefined' && JsonDataBinding.Debug) {
						_getFormClientHolder(form).value = DEBUG_SYMBOL + JSON.stringify(data);
					}
					else {
						_getFormClientHolder(form).value = JSON.stringify(data);
					}
					JsonDataBinding.SubmittedForm = form;
					state.serverPage = form.action;
					JsonDataBinding.SubmittedForm.state = state;
					if (JsonDataBinding.Debug) {
						JsonDataBinding.ShowDebugInfoLine('submit to :' + state.serverPage);
					}
					form.submit();
					return;
				}
			}
			state.serverPage = jsdb_serverPage;
			var xmlhttp;
			var ajaxWatcher;
			if (window.XMLHttpRequest) {
				// code for IE7+, Firefox, Chrome, Opera, Safari
				xmlhttp = new XMLHttpRequest();
			}
			else {
				// code for IE6, IE5
				xmlhttp = new ActiveXObject('Microsoft.XMLHTTP');
			}
			var ajaxCanceled = false;
			xmlhttp.onreadystatechange = function() {
				if (xmlhttp.readyState == 4) {
					if (typeof (ajaxWatcher) != 'undefined') {
						clearTimeout(ajaxWatcher);
					}
					if (xmlhttp.status == 200 || xmlhttp.status == 500) {
						_processServerResponse(xmlhttp.responseText, state);
						if (JsonDataBinding.startsWith(xmlhttp.responseText, 'PHP ')) {
							var s = xmlhttp.responseText.substr(4).trim();
							var idx = s.indexOf(':');
							if (idx > 0) {
								s = s.substr(idx + 1).trim();
								if (JsonDataBinding.startsWith(s, 'Maximum execution time of')) {
									onajaxtimeout();
								}
								else {
									if (document.onPhpFatalError) {
										JsonDataBinding.values.ServerError = xmlhttp.responseText;
										document.onPhpFatalError();
									}
								}
							}
						}
					}
					else {
						if (!JsonDataBinding.pageMoveout) {
							if (xmlhttp.status != 0 || JsonDataBinding.Debug) {
								if (!ajaxCanceled) {
									_processServerResponse((xmlhttp.status == 0 ? 'This web page must be served by a web server, not from a local file system. ' : '') + 'server call failed with status ' + xmlhttp.status + '. ' + xmlhttp.responseText, state, true);
									onajaxtimeout();
								}
							}
						}
					}
				}
			}
			var url = jsdb_serverPage + '?timeStamp=' + new Date().getTime();
			if (JsonDataBinding.Debug) {
				JsonDataBinding.ShowDebugInfoLine('send to :' + url);
			}
			xmlhttp.open('POST', url, true);
			xmlhttp.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
			if (execAttrs && execAttrs.headers) {
				for (var i = 0; i < execAttrs.headers.length; i++) {
					xmlhttp.setRequestHeader(execAttrs.headers[i].name, execAttrs.headers[i].value);
				}
			}
			//alert(data.toJSONString());
			if (JsonDataBinding.AjaxTimeout > 0) {
				function onajaxtimeout() {
					ajaxCanceled = true;
					xmlhttp.abort();
					if (document.onAjaxTimeout) {
						document.onAjaxTimeout();
					}
				}
				ajaxWatcher = setTimeout(onajaxtimeout, JsonDataBinding.AjaxTimeout * 1000);
			}
			if (JsonDataBinding.Debug) {
				xmlhttp.send(DEBUG_SYMBOL + JSON.stringify(data)); // data.toJSONString());
			}
			else {
				xmlhttp.send(JSON.stringify(data)); //data.toJSONString());
			}
		}
		_executeServerCommands = function(commands, clientProperties, data, form, execAttrs) {
			var req;
			if (JsonDataBinding.endsWithI(jsdb_serverPage, '.php')) {
				if (typeof clientProperties != 'undefined') {
					for (var nm in clientProperties) {
						if (clientProperties[nm] instanceof Date) {
							clientProperties[nm] = JsonDataBinding.datetimeToString(clientProperties[nm]);
						}
					}
				}
			}
			if (typeof data != 'undefined' && data != null) {
				if (typeof data == 'boolean') {
					if (data) {
						_sendBoundData('', clientProperties, commands);
					}
					else {
						req = new Object();
						req.Calls = commands;
						if (typeof clientProperties != 'undefined') {
							req.values = clientProperties;
						}
					}
				}
				else if (typeof data == 'string') {
					_sendBoundData(data, clientProperties, commands);
				}
				else {
					req = new Object();
					req.Calls = commands;
					if (typeof clientProperties != 'undefined') {
						req.values = clientProperties;
					}
					req.Data = data;
				}
			}
			else {
				req = new Object();
				req.Calls = commands;
				if (typeof clientProperties != 'undefined') {
					req.values = clientProperties;
				}
			}
			if (req) {
				_callServer(req, form, execAttrs);
			}
		}
		_getData = function(dataName, clientProperties) {
			var req = new Object();
			req.Calls = new Array(); //Calls is an array of server function calls. 'jsonDb_getData - ' is a special prefix for getting data
			req.Calls[0] = new Object();
			req.Calls[0].method = jsdb_getdata;
			req.Calls[0].value = dataName;
			if (typeof clientProperties != 'undefined') {
				req.values = clientProperties;
			}
			_callServer(req);
		}
		_attachOnRowDeleteHandler = function(name, handler) {
			if (typeof onrowdeletehandlers[name] == 'undefined') {
				onrowdeletehandlers[name] = new Array();
			}
			var exist = false;
			for (var i = 0; i < onrowdeletehandlers[name].length; i++) {
				if (onrowdeletehandlers[name][i] == handler) {
					exist = true;
					break;
				}
			}
			if (!exist) {
				onrowdeletehandlers[name].push(handler);
			}
		}
		function isEventSupported(el, eventName) {
			var isSupported = (eventName in el);
			if (!isSupported) {
				el.setAttribute(eventName, 'return;');
				isSupported = typeof el[eventName] == type_func;
			}
			return isSupported;
		}
		_getTableAttribute = function(tableName, attributeName) {
			if (tableAttributes) {
				if (tableAttributes[tableName]) {
					var attrs = tableAttributes[tableName];
					return attrs[attributeName];
				}
			}
		}
		_setTableAttribute = function(tableName, attributeName, value) {
			var attrs;
			if (!tableAttributes[tableName]) {
				tableAttributes[tableName] = new Object();
			}
			attrs = tableAttributes[tableName];
			attrs[attributeName] = value;
		}
		_addvaluechangehandler = function(tableName, handler) {
			var t = dataChangeHandlers[tableName];
			if (!t) {
				t = {};
				dataChangeHandlers[tableName] = t;
			}
			if (!t.onvaluechangehandlers) {
				t.onvaluechangehandlers = new Array();
			}
			for (var i = 0; i < t.onvaluechangehandlers.length; i++) {
				if (t.onvaluechangehandlers[i] == handler) {
					return;
				}
			}
			t.onvaluechangehandlers.push(handler);
		}
		_getvaluechangehandler = function(tableName) {
			var t = dataChangeHandlers[tableName];
			if (t) {
				return t.onvaluechangehandlers;
			}
		}
		//sendkeys support
		var focusedElement;
		var snedkeysinitialized;
		function saveFocused(e) {
			if (e) {
				if (e.tagName) {
					var tag = e.tagName.toLowerCase();
					if (tag == 'input') {
						var s = e.type ? e.type.toLowerCase() : '';
						if (s == 'text' || s == 'password') {
							focusedElement = e;
						}
					}
					else if (tag == 'textarea') {
						focusedElement = e;
					}
				}
			}
		}
		function onDocMouseDown(e) {
			var sender = JsonDataBinding.getSender(e);
			saveFocused(sender);
		}
		function onDocKeyup() {
			saveFocused(document.activeElement);
		}
		_initSendKeys = function() {
			if (!snedkeysinitialized) {
				snedkeysinitialized = true;
				if (!focusedElement) {
					_selectNextInput();
				}
				if (IsIE()) {
					JsonDataBinding.AttachEvent(document, "onfocusin", onDocKeyup);
				}
				else {
					document.addEventListener('focus', onDocKeyup, true);
				}
				//JsonDataBinding.AttachEvent(document, "onmousedown", onDocMouseDown);
				//JsonDataBinding.AttachEvent(document, "onkeyup", onDocKeyup);
			}
		}
		_selectNextInput = function() {
			var f = focusedElement;
			var currentTab = -100;
			if (f) {
				currentTab = f.tabIndex;
			}
			var gotNextTab = false;
			var gotMinTab = false;
			var nextTab;
			var minTab;
			var eNextTab;
			var eMinTab = f;
			function getNextTab(e) {
				for (var i = 0; i < e.childNodes.length; i++) {
					var a = e.childNodes[i];
					if (a && a.tabIndex && a.tagName) {
						var tag = a.tagName.toLowerCase();
						if ((tag == 'input' && a.type && a.type.toLowerCase() == 'text') || tag == 'textarea') {
							if (a.tabIndex > currentTab) {
								if (gotNextTab) {
									if (a.tabIndex < nextTab) {
										nextTab = a.tabIndex;
										eNextTab = a;
									}
								}
								else {
									nextTab = a.tabIndex;
									eNextTab = a;
									gotNextTab = true;
								}
							}
							else {
								if (gotMinTab) {
									if (minTab > a.tabIndex) {
										minTab = a.tabIndex;
										eMinTab = a;
									}
								}
								else {
									minTab = a.tabIndex;
									eMinTab = a;
									gotMinTab = true;
								}
							}
						}
					}
					getNextTab(a);
				}
			}
			getNextTab(document.body);
			if (gotNextTab) {
				eNextTab.focus();
				focusedElement = eNextTab;
			}
			else {
				if (gotMinTab) {
					eMinTab.focus();
					focusedElement = eMinTab;
				}
			}
			f = focusedElement;
			if (f) {
				var range;
				if (document.selection && document.selection.createRange) { //IE 8
					range = document.selection.createRange(); // a Text Range object
					if (range) {
						range.moveStart("character", f.value.length);
						range.moveEnd("character", f.value.length);
						range.select();
					}
				}
				else if (f.setSelectionRange || f.createTextRange) { //W3C standard
					var pos = f.value.length;
					if (f.setSelectionRange) {
						f.focus();
						f.setSelectionRange(pos, pos);
					}
					else if (f.createTextRange) {
						range = f.createTextRange();
						range.collapse(true);
						range.moveEnd('character', pos);
						range.moveStart('character', pos);
						range.select();
						f.focus();
					}
				}
			}
		}
		_sendKeys = function(key) {
			var f = focusedElement;
			if (f) {
				if (key == '{TAB}') {
					key = '\t';
				}
				f.focus();
				var range;
				if (document.selection && document.selection.createRange) { //IE 8
					range = document.selection.createRange(); // a Text Range object
					if (range) {
						range.text = key;
						range.collapse(false);
						range.select();
					}
				}
				else if (f.setSelectionRange || f.createTextRange) { //W3C standard
					if (f.setSelectionRange) {
						var len = f.value.length;
						var start = f.selectionStart;
						var end = f.selectionEnd;
						f.value = f.value.substring(0, start) + key + f.value.substring(end, len);
						var pos = start + key.length;
						f.focus();
						f.setSelectionRange(pos, pos);
					}
					else if (f.createTextRange) {
						range = f.createTextRange();
						range.collapse(true);
						range.moveEnd('character', pos);
						range.moveStart('character', pos);
						range.select();
					}
					f.focus();
				}
			}
		}
		//end of sendkeys support
	} (),
	//Enclosure finishes here ----------------------------------------
	refreshDataBind: function(e, name) {
		bindData({ childNodes: [e] }, name);
	},
	setDataBindSource: function (elementId, sourceName) {
		var e = document.getElementById(elementId);
		if (e) {
			e.setAttribute('jsdb', sourceName);
			if (isDataSourceValid(sourceName)) {
				bindData({ childNodes: [e.parentElement] }, sourceName, true);
			}
		}
	},
	createId: function(baseName) {
		return baseName + 'xxxxxxxx'.replace(/[x]/g, function(c) {
			var r = Math.random() * 16 | 0;
			return r.toString(16);
		});
	},
	//serverType: php or aspx
	//fileContents: whole contents of the file
	//filePath: path of the file to update
	//onFinish: handler for finishing update
	updateTextFile: function(serverType, fileContents, filePath, onFinish) {
		var serverPage = _getServerPage();
		if (serverType == 'php') {
			_setServerPage('limnor_updateFile.php');
		}
		else if (serverType == 'aspx') {
			_setServerPage('Limnor_webUtility.aspx');
		}
		else {
			if (onFinish) {
				onFinish('unsupported server type:' + serverType);
			}
			return;
		}
		var curId = JsonDataBinding.createId('id');
		_attachExtendedEvent('onProcessServerCall', curId, onFinish);
		_executeServerCommands([{ method: 'updateFile', value: filePath}], { contents: JsonDataBinding.base64Encode(fileContents), serverComponentName: curId });
		_setServerPage(serverPage);
	},
	deleteWebFile: function(serverType, filePath, onFinish) {
		var serverPage = _getServerPage();
		if (serverType == 'php') {
			_setServerPage('limnor_updateFile.php');
		}
		else if (serverType == 'aspx') {
			_setServerPage('Limnor_webUtility.aspx');
		}
		else {
			if (onFinish) {
				onFinish('unsupported server type:' + serverType);
			}
			return;
		}
		var curId = JsonDataBinding.createId('id');
		_attachExtendedEvent('onProcessServerCall', curId, onFinish);
		_executeServerCommands([{ method: 'deleteWebFile', value: filePath}], { serverComponentName: curId });
		_setServerPage(serverPage);
	},
	checkUrlExist: function(serverType, url, onFinish) {
		var serverPage = _getServerPage();
		if (serverType == 'php') {
			_setServerPage('limnor_updateFile.php');
		}
		else if (serverType == 'aspx') {
			_setServerPage('Limnor_webUtility.aspx');
		}
		else {
			if (onFinish) {
				onFinish('unsupported server type:' + serverType);
			}
			return;
		}
		var curId = JsonDataBinding.createId('id');
		_attachExtendedEvent('onProcessServerCall', curId, onFinish);
		_executeServerCommands([{ method: 'checkUrlExist', value: url}], { serverComponentName: curId });
		_setServerPage(serverPage);
	},
	checkFileExist: function(serverType, filepath, onFinish) {
		var serverPage = _getServerPage();
		if (serverType == 'php') {
			_setServerPage('limnor_updateFile.php');
		}
		else if (serverType == 'aspx') {
			_setServerPage('Limnor_webUtility.aspx');
		}
		else {
			if (onFinish) {
				onFinish('unsupported server type:' + serverType);
			}
			return;
		}
		var curId = JsonDataBinding.createId('id');
		_attachExtendedEvent('onProcessServerCall', curId, onFinish);
		_executeServerCommands([{ method: 'checkFileExist', value: filepath}], { serverComponentName: curId });
		_setServerPage(serverPage);
	},
	//clientProperties: an object with properties 
	accessServer: function(procPage, methodName, paramValue, clientProperties, onFinish) {
		var serverPage = _getServerPage();
		var curId = JsonDataBinding.createId('id');
		if (procPage) {
			_setServerPage(procPage);
		}
		_attachExtendedEvent('onProcessServerCall', curId, onFinish);
		if (clientProperties) {
			clientProperties.serverComponentName = curId;
		}
		else {
			clientProperties = { serverComponentName: curId };
		}
		_executeServerCommands([{ method: methodName, value: paramValue}], clientProperties);
		_setServerPage(serverPage);
	},
	//Data Binding API ===============================================
	setServerPage: function(pageUrl) {
		_setServerPage(pageUrl);
	},
	//dataName: identify the data to be fetched. server will interpret it
	//clientProperties: client values for providing query parameters
	getData: function(dataName, clientProperties) {
		_getData(dataName, clientProperties);
	},
	//dataName: identify the data to be submitted. if it is not given or it is '' then all modified data are submitted
	putData: function(dataName, clientProperties) {
		_putData(dataName, clientProperties);
	},
	collectModifiedData: function (dataName, polled) {
		return _collectModifiedData(dataName, polled);
	},
	//generic call to server, manually construct all the parameters
	//commands: an array of command: [{method, value}], method and value are strings
	//  use {method:'jsonDb_getData', value:'Table1'} to get data named Table1
	//  use {method:'jsonDb_putData', value:'Table1'} to submit modified data for Table1
	//  use {method:'myFunc1', value:'sun'} to call a server function signaled myFunc1 providing a string 'sun'
	//    usually a server page provides a function: serverProcess(method, value). The process engine calls this function
	//      providing method and value. The server page implement serverProcess according to business rules
	//clientProperties: an object as a property bag
	//data: it can be a DataSet, a Boolean, or a string.
	//  If it is a Boolean and it is true then the modified data will be collected.
	//  If it is a string then it is data name for submitting modified data. Use '' to submit all modified data.
	callServer: function(commands, clientProperties, data) {
		_executeServerCommands(commands, clientProperties, data);
	},
	executeServerMethod: function(command, clientProperties, form, data) {
		_executeServerCommands([{ method: command, value: '0'}], clientProperties, data, form);
	},
	sendRawData: function(data) {
		var debug = JsonDataBinding.Debug;
		JsonDataBinding.Debug = false;
		_callServer(data);
		JsonDataBinding.Debug = debug;
	},
	sendRawDataToURL: function(data, url, headers) {
		var debug = JsonDataBinding.Debug;
		var srv = _getServerPage();
		JsonDataBinding.Debug = false;
		_setServerPage(url);
		_callServer(data, null, headers);
		JsonDataBinding.Debug = debug;
		_setServerPage(srv);
	},
	submitBoundData: function() {
		_submitBoundData();
	},
	addRow: function(dataName) {
		return _addRow(dataName);
	},
	saveEdsToOffline: function (dataName, tag) {
		_saveEdsToOffline(dataName, tag);
	},
	loadEdsFromOffline: function (dataName, tag) {
		_loadEdsFromOffline(dataName, tag);
	},
	removeEdsFromOffline: function (tag) {
		_removeEdsFromOffline(tag);
	},
	deleteCurrentRow: function(dataName) {
		_deleteCurrentRow(dataName);
	},
	getCurrentRowIndex: function(dataName) {
		return _getCurrentRowIndex(dataName);
	},
	getRowCount: function(dataName) {
		return _getRowCount(dataName);
	},
	dataMoveFirst: function(dataName) {
		return _dataMoveFirst(dataName);
	},
	dataMovePrevious: function(dataName) {
		return _dataMovePrevious(dataName);
	},
	dataMoveNext: function(dataName) {
		return _dataMoveNext(dataName);
	},
	dataMoveLast: function(dataName) {
		return _dataMoveLast(dataName);
	},
	dataMoveToRecord: function(dataName, rowIndex) {
		return _dataMoveToRecord(dataName, rowIndex);
	},
	clearTableRows: function(dataName) {
		return _clearTableRows(dataName);
	},
	sortOnColumn: function(dataName, columnName, sortAsc, ignoreCase) {
		return _sortOnColumn(dataName, columnName, sortAsc, ignoreCase);
	},
	columnSum: function(dataName, columnName) {
		return _columnSum(dataName, columnName);
	},
	columnValue: function(dataName, columnName, rowIndex) {
		return _columnValue(dataName, columnName, rowIndex);
	},
	columnValueByIndex: function(dataName, columnIndex, rowIndex) {
		return _columnValueByIndex(dataName, columnIndex, rowIndex);
	},
	isColumnValueNull: function(dataName, columnName, rowIndex) {
		return _isColumnValueNull(dataName, columnName, rowIndex);
	},
	isColumnValueNullOrEmpty: function(dataName, columnName, rowIndex) {
		return _isColumnValueNullOrEmpty(dataName, columnName, rowIndex);
	},
	isColumnValueNotNull: function(dataName, columnName, rowIndex) {
		return _isColumnValueNotNull(dataName, columnName, rowIndex);
	},
	isColumnValueNotNullOrEmpty: function(dataName, columnName, rowIndex) {
		return _isColumnValueNotNullOrEmpty(dataName, columnName, rowIndex);
	},
	isColumnValueChanged: function (dataName, columnName, rowIndex) {
		return _isColumnValueChanged(dataName, columnName, rowIndex);
	},
	setColumnValue: function(dataName, columnName, val, rowIndex) {
		_setcolumnValue(dataName, columnName, val, rowIndex);
	},
	getColumnValue: function(dataName, columnName, rowIndex) {
		return _columnValue(dataName, columnName, rowIndex);
	},
	columnExpressionValue: function(dataName, expression, rowIndex) {
		return _columnExpressionValue(dataName, expression, rowIndex);
	},
	cancelDataEdit: function (dataName) {
		_cancelDataEdit(dataName);
	},
	statistics: function(dataName, expression, operator) {
		return _statistics(dataName, expression, operator);
	},
	addOnRowIndexChangeHandler: function(tableName, handler) {
		_addOnRowIndexChangeHandler(tableName, handler);
	},
	onRowIndexChange: function(name) {
		_onRowIndexChange(name);
	},
	refetchDetailRows: function(mainTableName, detailTableName) {
		_refetchDetailRows(mainTableName, detailTableName);
	},
	getTableBody: function(table) {
		var i;
		var tb; // = table.getElementsByTagName('tbody');
		for (i = 0; i < table.children.length; i++) {
			if (table.children[i] && table.children[i].tagName && table.children[i].tagName.toLowerCase() == 'tbody') {
				tb = table.children[i];
				break;
			}
		}
		if (!tb) {
			tb = document.createElement('tbody');
			var tf;
			for (i = 0; i < table.children.length; i++) {
				if (table.children[i] && table.children[i].tagName && table.children[i].tagName.toLowerCase() == 'tfoot') {
					tf = table.children[i];
					break;
				}
			}
			if (tf) {
				table.insertBefore(tb, tf);
			}
			else {
				table.appendChild(tb);
			}
		}
		return tb;
	},
	getSender: function(e) {
		var c;
		if (!e) e = window.event;
		if (e) {
			if (e.target) c = e.target;
			else if (e.srcElement) c = e.srcElement;
			if (typeof c != 'undefined') {
				if (c.nodeType == 3)
					c = c.parentNode;
			}
			else {
				c = e;
			}
		}
		return c;
	},
	getZOrder: function(e) {
		var z = 0;
		while (e) {
			if (e.style && e.style.zIndex) {
				var d = parseInt(e.style.zIndex);
				if (d > z) z = d;
			}
			e = e.parentNode;
		}
		return z;
	},
	AjaxTimeout: 0,
	values: {}, //server values downloaded
	Debug: false,
	SetEventFirer: function(eo) {
		if (typeof eo.disabled != 'undefined') {
			//            eo.disabled = true;
			_setEventFirer(eo);
		}
		else {
			_setEventFirer(null);
		}
	}, //the object firing an event which caused calling Ajax
	AttachOnRowDeleteHandler: function(name, handler) {
		_attachOnRowDeleteHandler(name, handler);
	},
	urlToFilename: function(url) {
		if (url) {
			return url.replace(/^.*(\\|\/|\:)/, '');
		}
	},
	getPageFilename: function() {
		var s = window.location.href; //http://localhost/filename.html?parameters
		//s = s.replace(/^.*(\\|\/|\:)/, '');
		//return s;
		return JsonDataBinding.urlToFilename(s);
	},
	getPageFilenameWithoutParameters: function() {
		var s = JsonDataBinding.getPageFilename();
		var pos = s.indexOf('?');
		if (pos > 0) {
			return s.substr(0, pos);
		}
		return s;
	},
	//subtest/test2.html
	getPageFileFullPath: function() {
		var s = window.location.pathname;
		if (s.charAt(0)) {
			s = s.substr(1);
		}
		return s;
	},
	//http://localhost/subtest/
	getWebSitePath: function() {
		var s = window.location.href;
		var f = JsonDataBinding.getPageFilename();
		var w = s.replace(f, '');
		return w;
	},
	gotoWebPage: function(pageFilepath) {
		if (pageFilepath) {
			var u1 = pageFilepath.toLowerCase();
			var wp = window;
			var alreadyLoaded = false;
			while (wp) {
				var u0 = wp.location.href;
				u0 = JsonDataBinding.urlToFilename(u0);
				var idx = u0.indexOf('?');
				if (idx > 0) {
					u0 = u0.substr(0, idx);
				}
				u0 = u0.toLowerCase();
				if (u0 == u1) {
					alreadyLoaded = true;
					break;
				}
				if (wp.parent == wp)
					break;
				if (!wp.parent)
					break;
				wp = wp.parent;
			}
			if (wp) {
				if (alreadyLoaded) {
					if (wp != window) {
						if (JsonDataBinding.closePage)
							JsonDataBinding.closePage();
					}
					else {
						if (IsFireFox()) {
							setTimeout('window.location.reload(true);', 0);
						}
						else {
							window.location.reload(true);
						}
					}
				}
				else {
					JsonDataBinding.pageMoveout = true;
					window.location.href = pageFilepath;
				}
			}
		}
	},
	eraseOfflineData: function (name) {
		if (window.localStorage) {
			var gv = window.localStorage['limnor_gv'];
			if (gv && gv.length > 0) {
				var vs = JSON.parse(gv);
				if (name in vs) {
					delete vs[name];
					gv = JSON.stringify(vs);
					window.localStorage['limnor_gv'] = gv;
				}
			}
		}
	},
	offlineDataExists: function (name) {
		if (window.localStorage) {
			var gv = window.localStorage['limnor_gv'];
			if (gv && gv.length > 0) {
				var vs = JSON.parse(gv);
				if (name in vs) {
					var d = vs[name];
					if (typeof (d) != 'undefined' && d !== null && typeof (d.v) != 'undefined' && d.v !== null) {
						if (typeof (d.e) != 'undefined' && d.e !== null) {
							d.e = new Date(d.e);
							if (d.e < new Date()) {
								delete vs[name];
								gv = JSON.stringify(vs);
								window.localStorage['limnor_gv'] = gv;
							}
							else
								return true;
						}
						else {
							return true;
						}
					}
				}
			}
		}
		return false;
	},
	cookieExists: function (name) {
		if (JsonDataBinding.UseLocalStore) {
			return JsonDataBinding.offlineDataExists(name);
		}
		//var nameEQ = name + "=";
		var ca = document.cookie.split(';');
		for (var i = 0; i < ca.length; i++) {
			var c = ca[i];
			//while (c.charAt(0) == ' ') c = c.substring(1, c.length);
			//if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length, c.length);
			var pos = c.indexOf('=');
			if (pos > 0) {
				var nm = c.substr(0, pos).replace(/^\s+|\s+$/g, "");
				if (nm == name) {
					return true;
				}
			}
		}
		return false;
	},
	getOfflineDataByStartsWith: function (name) {
		var ret = new Array();
		if (window.localStorage) {
			var gv = window.localStorage['limnor_gv'];
			if (gv && gv.length > 0) {
				var vs = JSON.parse(gv);
				for (var nm in vs) {
					if (JsonDataBinding.startsWithI(nm, name)) {
						var vl = vs[nm];
						if (typeof (vl) != 'undefined' && vl !== null) {
							if (typeof (vl.e) != 'undefined' && vl.e !== null) {
								vl.e = new Date(vl.e);
								if (vl.e < new Date()) {
									delete vs[nm];
									gv = JSON.stringify(vs);
									window.localStorage['limnor_gv'] = gv;
									continue;
								}
							}
							ret.push({ name: nm, value: vl.v });
						}
					}
				}
			}
		}
		return ret;
	},
	getCookieByStartsWith: function (name) {
		if (JsonDataBinding.UseLocalStore) {
			return JsonDataBinding.getOfflineDataByStartsWith(name);
		}
		var ret = new Array();
		var ca = document.cookie.split(';');
		for (var i = 0; i < ca.length; i++) {
			var c = ca[i];
			var pos = c.indexOf('=');
			if (pos > 0) {
				var nm = c.substr(0, pos).replace(/^\s+|\s+$/g, "");
				if (JsonDataBinding.startsWithI(nm, name)) {
					ret.push({ name: nm, value: c.substr(pos + 1) });
				}
			}
		}
		return ret;
	},
	getOfflineData: function (name) {
		if (window.localStorage) {
			var gv = window.localStorage['limnor_gv'];
			if (gv && gv.length > 0) {
				var vs = JSON.parse(gv);
				if (name in vs) {
					var vl = vs[name];
					if (typeof (vl) != 'undefined' && vl !== null) {
						if (typeof (vl.e) != 'undefined' && vl.e !== null) {
							vl.e = new Date(vl.e);
							var dn = new Date();
							if (vl.e < dn) {
								delete vs[name];
								gv = JSON.stringify(vs);
								window.localStorage['limnor_gv'] = gv;
								return;
							}
						}
						return vl.v;
					}
				}
			}
		}
	},
	getCookie: function (name) {
		if (JsonDataBinding.UseLocalStore) {
			return JsonDataBinding.getOfflineData(name);
		}
		var ca = document.cookie.split(';');
		for (var i = 0; i < ca.length; i++) {
			var c = ca[i];
			var pos = c.indexOf('=');
			if (pos > 0) {
				var nm = c.substr(0, pos).replace(/^\s+|\s+$/g, "");
				if (nm == name) {
					return c.substr(pos + 1);
				}
			}
		}
		return null;
	},
	setOfflineData: function (name, value, exMinutes) {
		if (window.localStorage) {
			var vs = {};
			var gv = window.localStorage['limnor_gv'];
			if (gv && gv.length > 0) {
				vs = JSON.parse(gv);
			}
			var da = {};
			da.v = value;
			if (exMinutes) {
				var date = new Date();
				//date.setTime(date.getTime() + (parseInt(exMinutes) * 60 * 1000));
				da.e = new Date(date.getTime() + parseInt(exMinutes) * 60 * 1000);
			}
			vs[name] = da;
			gv = JSON.stringify(vs);
			window.localStorage['limnor_gv'] = gv;
		}
	},
	setCookie: function (name, value, exMinutes) {
		if (JsonDataBinding.UseLocalStore) {
			JsonDataBinding.setOfflineData(name, value, exMinutes);
		}
		else {
			var expires;
			if (exMinutes) {
				var date = new Date();
				date.setTime(date.getTime() + (parseInt(exMinutes) * 60 * 1000));
				expires = "; expires=" + date.toGMTString();
			}
			else expires = "";
			var ck = name + "=" + value + expires + "; path=/;";
			//alert(ck);
			document.cookie = ck;
		}
	},
	eraseCookie: function (name) {
		if (JsonDataBinding.UseLocalStore) {
			JsonDataBinding.eraseOfflineData(name);
		}
		else {
			JsonDataBinding.setCookie(name, "", -1);
		}
	},
	getReturnUrl: function() {
		var s = window.location.href;
		var n = s.indexOf('?');
		if (n >= 0) {
			s = s.substr(n + 1);
			if (JsonDataBinding.endsWith(s, '$')) {
				s = s.substr(0, s.length - 1);
			}
			if (!JsonDataBinding.startsWithI(s, 'debugRef=')) {
				return s;
			}
		}
	},
	ShowPermissionError: function(labelName, msg) {
		var s = window.location.href;
		var n = s.indexOf('?');
		if (n >= 0) {
			if (JsonDataBinding.endsWith(s, '$')) {
				if (!msg || msg.length == 0) {
					msg = 'You do not have permission to visit this web page.';
				}
				if (labelName && labelName.length > 0) {
					var lbl = document.getElementById(labelName);
					if (lbl) {
						lbl.innerHTML = msg;
						return;
					}
				}
				_executeClientEventObject('LoginFailed');
				//var eho = _getClientEventObject('LoginFailed');
				//if (eho) {
				//    eho.LoginFailed();
				//}
			}
		}
	},
	hasLoggedOn: function() {
		return _hasLoggedOn();
	},
	LoginFailed: function(msgId, msg) {
		var lbl = document.getElementById(msgId);
		JsonDataBinding.SetInnerText(lbl, msg);
		_executeClientEventObject('LoginFailed');
		//var eho = _getClientEventObject('LoginFailed');
		//if (eho) {
		//    eho.LoginFailed();
		//}
	},
	LoginPassed: function(login, expire, userLevel, userid) {
		try {
			_loginPassed(login, expire, userLevel, userid);
			if (typeof JsonDataBinding != 'undefined') {
				var surl = JsonDataBinding.getReturnUrl();
				if (surl) {
					window.location.href = surl;
				}
			}
		}
		catch (err) {
			alert('Error handling post-login. ' + (err.message ? err.message : err));
		}
	},
	LoginPassed2: function() {
		if (typeof JsonDataBinding != 'undefined') {
			var surl = JsonDataBinding.getReturnUrl();
			if (surl) {
				window.location.href = surl;
			}
		}
	},
	LogOff: function() {
		_logOff();
	},
	LogonPage: '',
	setupLoginWatcher: function() {
		_setupLoginWatcher();
	},
	TargetUserLevel: 0,
	GetCurrentUserAlias: function() {
		return _getCurrentUserAlias();
	},
	GetCurrentUserID: function() {
		return _getCurrentUserID();
	},
	GetCurrentUserLevel: function() {
		return _getCurrentUserLevel();
	},
	UserLoggedOn: function() {
		return _userLoggedOn();
	},
	SetLoginCookieName: function(nm) {
		_setUserLogCookieName(nm);
	},
	IsChrome: function() {
		return (navigator.userAgent.toLowerCase().indexOf('chrome') > -1);
	},
	IsSafari: function() {
		return IsSafari();
	},
	IsIE: function() {
		return IsIE();
	},
	IsFireFox: function() {
		return IsFireFox();
	},
	IsOpera: function() {
		return IsOpera();
	},
	// Returns the version of Internet Explorer or a -1
	// (indicating the use of another browser).
	getInternetExplorerVersion: function() {
		var rv = -1; // Return value assumes failure.
		if (navigator.appName == 'Microsoft Internet Explorer') {
			var ua = navigator.userAgent;
			var re = new RegExp("MSIE ([0-9]{1,}[\.0-9]{0,})");
			if (re.exec(ua) != null)
				rv = parseFloat(RegExp.$1);
		}
		return rv;
	},
	SimpleHandlerChain: function(previous, current) {
		var pre = previous;
		var cur = current;
		return function() {
			pre();
			cur();
		}
	},
	getPageZIndex: function(e0) {
		function _getzIndex(e, zi) {
			if (e != e0) {
				var zx = 0;
				for (var i = 0; i < e.childNodes.length; i++) {
					var a = e.childNodes[i];
					if (a.style && a.style.zIndex) {
						zx = parseInt(a.style.zIndex);
						if (zx > zi) {
							zi = zx;
						}
					}
					var z2 = _getzIndex(a, zi);
					if (z2 > zi) {
						zi = z2;
					}
				}
			}
			return zi;
		}
		return _getzIndex(document.body, 0);
	},
	AttachEvent: function(obj, eventName, handler) {
		if (!obj || !eventName || !handler)
			return;
		if (eventName == '') return;
		if (!obj.handlerHolders)
			obj.handlerHolders = {};
		if (!obj.handlerHolders[eventName])
			obj.handlerHolders[eventName] = [];
		obj.handlerHolders[eventName].push(handler);
		if (IsFireFox() || IsSafari() || IsChrome() || IsOpera()) {
			if (eventName.substring(0, 2) == 'on') {
				eventName = eventName.substring(2);
			}
		}
		if (typeof (obj.attachEvent) == 'function') {
			obj.attachEvent(eventName, handler);
		}
		else {
			if (typeof (obj.addEventListener) == 'function') {
				if (eventName.substring(0, 2) == 'on') {
					eventName = eventName.substring(2);
				}
				obj.addEventListener(eventName, handler, false);
			}
			else {
				if (obj[eventName]) {
					obj[eventName] = JsonDataBinding.SimpleHandlerChain(obj[eventName], handler);
				}
				else {
					obj[eventName] = handler;
				}
			}
		}
	},
	DetachEvent: function(obj, eventName, handler) {
		if (obj.handlerHolders && obj.handlerHolders[eventName]) {
			for (var i = 0; i < obj.handlerHolders[eventName].length; i++) {
				if (obj.handlerHolders[eventName][i] == handler) {
					obj.handlerHolders[eventName].splice(i, 1);
					break;
				}
			}
		}
		if (IsFireFox() || IsSafari() || IsChrome() || IsOpera()) {
			if (eventName.substring(0, 2) == 'on') {
				eventName = eventName.substring(2);
			}
		}
		if (typeof (obj.detachEvent) == 'function') {
			obj.detachEvent(eventName, handler);
		}
		else {
			if (typeof (obj.removeEventListener) == 'function') {
				obj.removeEventListener(eventName, handler, false);
			}
			else {
				if (obj[eventName]) {
					obj[eventName] = null; // JsonDataBinding.RemmoveSimpleHandlerChain(obj[eventName], handler);
				}
			}
		}
	},
	ClearEvent: function(obj, eventName) {
		var en = eventName;
		if (IsFireFox() || IsSafari() || IsChrome() || IsOpera()) {
			if (en.substring(0, 2) == 'on') {
				en = en.substring(2);
			}
		}
		if (obj[eventName]) {
			obj[eventName] = null;
		}
		if (obj.handlerHolders && obj.handlerHolders[eventName]) {
			for (var i = 0; i < obj.handlerHolders[eventName].length; i++) {
				if (obj.handlerHolders[eventName][i]) {
					if (typeof (obj.detachEvent) == 'function') {
						obj.detachEvent(en, obj.handlerHolders[eventName][i]);
					}
					if (typeof (obj.removeEventListener) == 'function') {
						obj.removeEventListener(en, obj.handlerHolders[eventName][i], false);
					}
				}
			}
			obj.handlerHolders[eventName] = null;
		}
	},
	SwitchCulture: function(cultreName) {
		if (typeof cultreName == 'undefined' || cultreName == null || cultreName == 'null') {
			cultreName = _getCulture();
		}
		if (!cultreName || cultreName == 'null') {
			cultreName = '';
		}
		_setCulture(cultreName);
	},
	GetCulture: function() {
		return _getCulture();
	},
	GetValueInCurrentCulture: function(valueName) {
		return _getValueInCurrentCulture(valueName);
	},
	AddPageResourceName: function(resName, resType) {
		_addPageResourceName(resName, resType);
	},
	AddPageCulture: function(cultureName) {
		_addPageCulture(cultureName);
	},
	PageValues: Object,
	ProcessPageParameters: function() {
		var fname;
		var purl;
		if (typeof JsonDataBinding.PageValues == 'undefined') {
			JsonDataBinding.PageValues = new Object();
		}
		var query = window.location.search.substring(1);
		var vars = query.split("&");
		for (var i = 0; i < vars.length; i++) {
			var pair = vars[i].split("=");
			if (pair && pair.length > 0) {
				if (pair.length == 1) {
					JsonDataBinding.PageValues['P10936C6EB1D741fbA2B8A25A7E2B61EF'] = unescape(pair[0]);
				}
				else if (pair.length == 2) {
					JsonDataBinding.PageValues[pair[0]] = unescape(pair[1]);
					if (pair[0].substr(0, 7) == 'iframe_') {
						fname = pair[0].substr(7);
						purl = JsonDataBinding.PageValues[pair[0]];
					}
					else if (pair[0] == 'lang') {
						JsonDataBinding.SwitchCulture(pair[1]);
					}
				}
			}
		}
		JsonDataBinding.anchorAlign.initializeBodyAnchor();
		JsonDataBinding.AttachEvent(window, 'onresize', JsonDataBinding.anchorAlign.applyBodyAnchorAlign);
		JsonDataBinding.anchorAlign.applyBodyAnchorAlign();
		if (typeof fname != 'undefined' && fname.length > 0) {
			window.open(purl, fname);
		}
	},
	onPageInitialize: function() {
		if (document.onparentload) {
			function checkParentReady() {
				if (window.parent) {
					if (window.parent.document.readyState != 'complete') {
						setTimeout(checkParentReady, 200);
						return;
					}
					document.onparentload();
				}
			}
			checkParentReady();
		}
	},
	SetImageData: function(c, v) {
		if (v) {
			c.src = 'data:image/jpg;base64,' + v;
		}
		else {
			c.src = null;
		}
	},
	SetInnerText: function(c, v0) {
		if (c && c.tagName) {
			var v = JsonDataBinding.toText(v0);
			if (c.tagName.toLowerCase() == 'input') {
				c.value = v;
			}
			else {
				if (IsIE()) {
					c.innerText = v;
				}
				else if (typeof (c.textContent) == 'undefined') {
					c.innerText = v;
				}
				else {
					c.textContent = v;
				}
			}
		}
	},
	GetInnerText: function(c) {
		if (c && c.tagName) {
			if (c.tagName.toLowerCase() == 'input') {
				return c.value;
			}
			else {
				if (IsIE()) {
					return c.innerText;
				}
				else if (typeof (c.textContent) == 'undefined') {
					return c.innerText;
				}
				else {
					return c.textContent;
				}
			}
		}
	},
	GetSelectedListValue: function(list) {
		if (list.selectedIndex >= 0) {
			return list.options[list.selectedIndex].value;
		}
		return null;
	},
	GetSelectedListText: function(list) {
		if (list.selectedIndex >= 0) {
			return list.options[list.selectedIndex].text;
		}
		return '';
	},
	SetSelectedListValue: function(list, v) {
		if (list.selectedIndex >= 0) {
			list.options[list.selectedIndex].value = v;
		}
		return null;
	},
	SetSelectedListText: function(list, v) {
		if (list.selectedIndex >= 0) {
			list.options[list.selectedIndex].text = v;
		}
		return '';
	},
	SetTextHeightToContent: function(ta) {
		function resize() {
			ta.style.height = 'auto';
			ta.style.height = ta.scrollHeight + 'px';
			if (ta.onHeightAdjusted) {
				ta.onHeightAdjusted(ta);
			}
		}
		window.setTimeout(resize, 0);
	},
	ProcessServerResponse: function(r) {
		_processServerResponse(r);
	},
	IFrame: null,
	SubmittedForm: null,
	ProcessIFrame: function() {
		if (typeof JsonDataBinding != 'undefined') {
			if (JsonDataBinding.IFrame) {
				try {
					if (JsonDataBinding.IFrame.document) {
						if (JsonDataBinding.SubmittedForm && JsonDataBinding.SubmittedForm.state)
							_processServerResponse(JsonDataBinding.IFrame.document.body.innerHTML, JsonDataBinding.SubmittedForm.state);
						else
							_processServerResponse(JsonDataBinding.IFrame.document.body.innerHTML);
						JsonDataBinding.IFrame.document.body.innerHTML = '';
					}
				}
				catch (exp) {
					if (typeof JsonDataBinding != 'undefined') {
						if (JsonDataBinding.SubmittedForm && JsonDataBinding.SubmittedForm.state)
							_processServerResponse('Error processing form submit. ' + (exp.message ? exp.message : exp) + '. You may try to use Chrome to get more detailed and accurate information', JsonDataBinding.SubmittedForm.state, true);
						else
							_processServerResponse('Error processing form submit. ' + (exp.message ? exp.message : exp) + '. You may try to use Chrome to get more detailed and accurate information', true);
					}
				}
			}
		}
	},
	GetSelectedText: function() {
		var userSelection;
		if (window.getSelection) {
			userSelection = window.getSelection();
		}
		else if (document.selection) { // should come last; Opera!
			userSelection = document.selection.createRange();
		}
		//var rangeObject = getRangeObject(userSelection);
		var selectedText = userSelection;
		if (userSelection.text)
			selectedText = userSelection.text;
		else {
			if (userSelection.anchorNode) {
				selectedText = userSelection.anchorNode.nodeValue;
			}
		}
		return selectedText;
	},
	ShowAjaxCallWaitingImage: null,
	ShowAjaxCallWaitingLabel: null,
	SetDatetimePicker: function(datetimePicker) {
		_setDatetimePicker(datetimePicker);

	},
	GetDatetimePicker: function() {
		return _getdatetimepicker();
	},
	GetDatetimePickerSelectedValue: function(textBoxId) {
		var dp = JsonDataBinding.GetDatetimePicker();
		if (dp) {
			return dp.getSelectedDate(textBoxId);
		}
	},
	SetDatetimePickerSelectedValue: function(textBoxId, d) {
		var dp = JsonDataBinding.GetDatetimePicker();
		if (dp) {
			if (d) {
				var yyyymmdd;
				var hh, mm, ss;
				if (d instanceof Date) {
					var mo = d.getMonth() + 1;
					yyyymmdd = '' + d.getFullYear() + (mo < 10 ? '0' + mo : mo) + (d.getDate() < 10 ? '0' + d.getDate() : d.getDate());
					hh = d.getHours();
					mm = d.getMinutes();
					ss = d.getSeconds();
				}
				else {
					d = JsonDataBinding.replaceAll(d, '-', '');
					var i = d.indexOf(' ');
					if (i > 0) {
						yyyymmdd = d.substr(0, i);
						var hhmmss = d.substr(i + 1);
						if (hhmmss && hhmmss.length > 0) {
							i = hhmmss.indexOf(':');
							if (i >= 0) {
								hh = hhmmss.substr(0, i);
								hhmmss = hhmmss.substr(i + 1);
								i = hhmmss.indexOf(':');
								if (i >= 0) {
									mm = hhmmss.substr(0, i);
									ss = hhmmss.substr(i + 1);
								}
							}
							else {
								if (hhmmss.length == 6) {
									hh = hhmmss.substr(0, 2);
									mm = hhmmss.substr(2, 2);
									ss = hhmmss.substr(4, 2);
								}
							}
						}
					}
					else {
						yyyymmdd = d;
					}
				}
				if (yyyymmdd) {
					var b = dp.setSelectedDate(textBoxId, yyyymmdd, hh, mm, ss);
					return b;
				}
			}
		}
	},
	DisableDatetimePicker: function(textBoxId, d) {
		var dp = JsonDataBinding.GetDatetimePicker();
		if (dp) {
			if (d)
				dp.disable(textBoxId);
			else
				dp.enable(textBoxId);
		}
	},
	IsDatetimePickerIncludeTime: function(textBoxId) {
		var dp = JsonDataBinding.GetDatetimePicker();
		if (dp) {
			return dp.getEnableTime(textBoxId);
		}
	},
	SetDatetimePickerUseTime: function(textBoxId, d) {
		var dp = JsonDataBinding.GetDatetimePicker();
		if (dp) {
			dp.setEnableTime(textBoxId, d);
		}
	},
	adjustDatePickerButtonPos: function(textBoxId) {
		var dp = JsonDataBinding.GetDatetimePicker();
		if (dp) {
			dp.adjustButtonPos(textBoxId);
		}
	},
	showHideDatePicker: function(show, inpId) {
		var dp = JsonDataBinding.GetDatetimePicker();
		if (dp) {
			if (show)
				dp.show(inpId);
			else
				dp.hide(inpId);
		}
	},
	CreateDatetimePickerForTextBox: function(textBoxId, fontsize, includeTime, isStandalone, container, disableMove) {
		var dp = JsonDataBinding.GetDatetimePicker();
		if (dp) {
			function onselecteddatetime(args) {
				if (args && args.id) {
					var o = document.getElementById(args.id);
					if (o && o.onselectedDateTime) {
						o.onselectedDateTime({ target: o });
					}
				}
			}
			var fsize = (typeof fontsize == 'undefined' ? '12px' : fontsize);
			var bTime = typeof includeTime == 'undefined' ? true : includeTime;
			var bStandalone = typeof isStandalone == 'undefined' ? false : isStandalone;
			var pos = bStandalone ? 'static' : 'absolute';
			var opts = {
				formElements: {},
				showWeeks: true,
				statusFormat: "l-cc-sp-d-sp-F-sp-Y",
				bounds: { position: pos, inputRight: true, fontSize: fsize, inputTime: bTime, forStandalone: bStandalone }
			};
			opts.formElements[textBoxId] = "Y-ds-m-ds-d";
			opts.callbackFunctions = { 'dateset': [onselecteddatetime] };
			if (container) {
				opts.bounds.container = container;
			}
			if (disableMove) {
				opts.bounds.disableMove = disableMove;
			}
			dp.createDatePicker(opts);
			if (bStandalone) {
				dp.show(textBoxId);
				var el = document.getElementById(textBoxId);
				if (el) {
					el.datepickerStandaloe = true;
					var dpdiv = document.getElementById('fd-' + textBoxId);
					if (dpdiv) {
						dpdiv.style.left = el.style.left;
						dpdiv.style.top = el.style.top;
					}
					el.style.display = 'none';
					var but = document.getElementById("fd-but-" + textBoxId);
					if (but) {
						but.style.display = 'none';
					}
				}
			}
		}
		else {
			_pushDatetimeInput({ textBoxId: textBoxId, fontsize: fontsize, inputTime: includeTime, standalone: isStandalone, container: container, disableMove: disableMove });
		}
	},
	getClientEventHolder: function(eventName, objectName) {
		return _getClientEventHolder(eventName, objectName);
	},
	attachExtendedEvent: function(eventName, objectName, handler) {
		_attachExtendedEvent(eventName, objectName, handler);
	},
	detachExtendedEvent: function(eventName, objectName, handler) {
		_detachExtendedEvent(eventName, objectName, handler);
	},
	clearExtendedEvent: function(eventName, objectName) {
		_clearExtendedEvent(eventName, objectName);
	},
	switchExtendedEvent: function(eventName, objectName, handler) {
		_clearExtendedEvent(eventName, objectName);
		_attachExtendedEvent(eventName, objectName, handler)
	},
	executeEventHandlers: function(eventName, objectName) {
		_executeEventHandlers(eventName, objectName);
	},
	getObjectProperty: function(objectName, propertyName) {
		return _getObjectProperty(objectName, propertyName);
	},
	setObjectProperty: function(objectName, propertyName, value) {
		_setObjectProperty(objectName, propertyName, value);
	},
	onSetCustomValue: function(obj, valueName) {
		_onSetCustomValue(obj, valueName);
	},
	eraseSessionVariable: function(name) {
		_eraseSessionVariable(name);
	},
	getSessionVariable: function(name) {
		return _getSessionVariable(name);
	},
	setSessionVariable: function(name, value) {
		_setSessionVariable(name, value);
	},
	initSessionVariable: function(name, value) {
		_initSessionVariable(name, value);
	},
	StartSessionWatcher: function() {
		_startSessionWatcher();
	},
	GetSessionVariables: function() {
		return _getSessionVariables();
	},
	setSessionTimeout: function(timeoutMinutes) {
		_setSessionTimeout(timeoutMinutes);
	},
	getSessionTimeout: function() {
		return _getSessionTimeout();
	},
	bindDataToElement: function(e, name, firstTime) {
		_bindData(e, name, firstTime);
	},
	resetDataStreaming: function(name) {
		_resetDataStreaming(name);
	},
	//setDataStreaming: function (name) {
	//    _setDataStreaming(name);
	//},
	getModifiedRowCount: function(name) {
		return _getModifiedRowCount(name);
	},
	getDeletedRowCount: function(name) {
		return _getDeletedRowCount(name);
	},
	getNewRowCount: function(name) {
		return _getNewRowCount(name);
	},
	getRowCount: function(name) {
		return _getRowCount(name);
	},
	getActiveRowCount: function(name) {
		return _getActiveRowCount(name);
	},
	setTableAttribute: function(tableName, attributeName, value) {
		_setTableAttribute(tableName, attributeName, value);
	},
	getTableAttribute: function(tableName, attributeName) {
		return _getTableAttribute(tableName, attributeName);
	},
	addTableLink: function(tableName, value) {
		var detailTbls = _getTableAttribute(tableName, 'LinkedDetails');
		if (!detailTbls) {
			detailTbls = [];
			_setTableAttribute(tableName, 'LinkedDetails', detailTbls);
		}
		for (var i = 0; i < detailTbls.length; i++) {
			if (detailTbls[i].detailTableName == value.detailTableName) {
				detailTbls[i] = value;
				return;
			}
		}
		detailTbls.push(value);
	},
	confirmResult: false,
	addvaluechangehandler: function(tableName, handler) {
		_addvaluechangehandler(tableName, handler);
	},
	onvaluechanged: function(t, r, c, val) {
		var ta = _getvaluechangehandler(t.TableName);
		if (ta) {
			for (var i = 0; i < ta.length; i++) {
				if (ta[i]) {
					ta[i].oncellvaluechange(t.TableName, r, c, val);
				}
			}
		}
		_setTableAttribute(t.TableName, 'LastEditRow', r);
		_setTableAttribute(t.TableName, 'LastEditColumn', c);
		_executeEventHandlers('DataEdited', t.TableName);
	},
	endsWith: function(container, ends) {
		if (container && ends) {
			if (container.length >= ends.length) {
				return container.indexOf(ends, container.length - ends.length) !== -1;
			}
		}
		return false;
	},
	endsWithI: function(container, ends) {
		if (container && ends) {
			if (container.length >= ends.length) {
				var c = container.toLowerCase();
				var e = ends.toLowerCase();
				return c.indexOf(e, c.length - e.length) !== -1;
			}
		}
		return false;
	},
	startsWith: function(container, starts) {
		if (container && starts) {
			if (container.length >= starts.length) {
				var c = container.substr(0, starts.length);
				return (c == starts);
			}
		}
		return false;
	},
	startsWithI: function(container, starts) {
		if (container && starts) {
			if (container.length >= starts.length) {
				var c = container.substr(0, starts.length).toLowerCase();
				var e = starts.toLowerCase();
				return (c == e);
			}
		}
		return false;
	},
	stringEQi: function(s1, s2) {
		if (s1 && s2) {
			if (typeof s1 == 'string') {
				if (typeof s2 == 'string') {
					return (s1.toLowerCase() == s2.toLowerCase());
				}
				else {
					var s20 = s2.toString().toLowerCase();
					return s1 == s20;
				}
			}
			else {
				var s10 = s1.toString();
				if (typeof s2 == 'string') {
					return s10 == s2;
				}
			}
		}
		return (s1 === s2);
	},
	datetimeToString: function(d) {
		if (d instanceof Date) {
			var mo = d.getMonth() + 1;
			var dy = d.getDate();
			var hr = d.getHours();
			var mi = d.getMinutes();
			var se = d.getSeconds();
			return d.getFullYear() + "-" + (mo < 10 ? "0" + mo : mo) + "-" + (dy < 10 ? "0" + dy : dy) + " " + (hr < 10 ? "0" + hr : hr) + ":" + (mi < 10 ? "0" + mi : mi) + ":" + (se < 10 ? "0" + se : se);
		}
		return d;
	},
	getAlphaNumeric: function(s) {
		if (s) {
			return s.replace(/[^\w]+/g, "");
		}
		return s;
	},
	getAlphaNumericEx: function(s) {
		if (s) {
			return s.replace(/[^a-zA-Z0-9_-]+/g, "");
		}
		return s;
	},
	getAlphaNumericPlus: function(s) {
		if (s) {
			return s.replace(/[^a-zA-Z0-9 _+-]+/g, "");
		}
		return s;
	},
	randomString: function(string_length) {
		var chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXTZabcdefghiklmnopqrstuvwxyz";
		var randomstring = '';
		for (var i = 0; i < string_length; i++) {
			var rnum = Math.floor(Math.random() * chars.length);
			randomstring += chars.substring(rnum, rnum + 1);
		}
		return randomstring;
	},
	replaceAll: function(str, token, newToken, ignoreCase) {
		if (str && token) {
			str = str + '';
			token = token + '';
			if (str.length > 0 && token.length > 0) {
				var _token;
				var i = -1;
				if (typeof newToken == 'undefined' || newToken == null) newToken = '';
				if (ignoreCase) {
					_token = token.toLowerCase();
					while ((
						i = str.toLowerCase().indexOf(
							token, i >= 0 ? i + newToken.length : 0
						)) !== -1
					) {
						str = str.substring(0, i) +
							newToken +
							str.substring(i + token.length);
					}

				} else {
					return str.split(token).join(newToken);
				}
			}
		}
		return str;
	},
	getFilename: function(f) {
		if (typeof f != 'undefined') {
			var pos = f.lastIndexOf('/');
			if (pos >= 0) {
				f = f.substr(pos + 1);
			}
			pos = f.lastIndexOf('\\');
			if (pos >= 0) {
				f = f.substr(pos + 1);
			}
			return f;
		}
	},
	getFilenameNoExt: function(f) {
		f = JsonDataBinding.getFilename(f);
		if (typeof f != 'undefined') {
			var pos = f.lastIndexOf('.');
			if (pos >= 0) {
				f = f.substr(pos + 1);
			}
			return f;
		}
	},
	removeArrayItem: function(oa, v) {
		var ret = new Array();
		if (oa && oa.length > 0) {
			for (var i = 0; i < oa.length; i++) {
				if (oa[i] != v) {
					ret.push(oa[i]);
				}
			}
		}
		return ret;
	},
	removeEmptyArrayItem: function(oa) {
		var ret = new Array();
		if (oa && oa.length > 0) {
			for (var i = 0; i < oa.length; i++) {
				if (typeof oa[i] != 'undefined' && oa[i] != null && oa[i] != '') {
					ret.push(oa[i]);
				}
			}
		}
		return ret;
	},
	isEmailAddress: function(email) {
		var re = /^(([^<>()[\]\\.,;:\s@\"]+(\.[^<>()[\]\\.,;:\s@\"]+)*)|(\".+\"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
		return re.test(email);
	},
	isEmailAddressList: function(emails) {
		if (typeof emails == 'string' && emails != null && emails.length > 0) {
			var ss = emails.split(';');
			for (var i = 0; i < ss.length; i++) {
				if (ss[i].length > 0 && !JsonDataBinding.isEmailAddress(ss[i])) {
					return false;
				}
			}
			return true;
		}
		return false;
	},
	getContentSize: function(el, adjustHeight) {
		var sz = { x: 0, y: 0 };
		function getsize(e) {
			if (e) {
				for (var i = 0; i < e.children.length; i++) {
					if (e.children[i].offsetLeft + e.children[i].scrollWidth > sz.x) {
						sz.x = e.children[i].offsetLeft + e.children[i].scrollWidth;
					}
					if (e.children[i].offsetTop + e.children[i].scrollHeight > sz.y) {
						sz.y = e.children[i].offsetTop + e.children[i].scrollHeight;
					}
					if (adjustHeight) {
						e.children[i].style.height = e.children[i].scrollHeight + 'px';
					}
					getsize(e.children[i]);
				}
			}
		}
		getsize(el);
		return sz;
	},
	setFont: function(e, ft) {
		var fnt = { fontStyle: '', fontWeight: '', textDecoration: '' }
		for (var i = 0; i < ft.length; i++) {
			fnt[ft[i].name] = ft[i].value;
		}
		for (var nm in fnt) {
			e.style[nm] = fnt[nm];
		}
	},
	decodeBase64: function(input) {
		if (!input) {
			return input;
		}
		var bytes = [];
		var output = "";
		var chr1, chr2, chr3 = "";
		var enc1, enc2, enc3, enc4 = "";
		var i = 0;
		var keyStr = "ABCDEFGHIJKLMNOP" +
                "QRSTUVWXYZabcdef" +
                "ghijklmnopqrstuv" +
                "wxyz0123456789+/" +
                "=";

		// remove all characters that are not A-Z, a-z, 0-9, +, /, or =
		input = input.replace(/[^A-Za-z0-9\+\/\=]/g, "");
		if (input.length == 0) {
			return input;
		}
		do {
			enc1 = keyStr.indexOf(input.charAt(i++));
			enc2 = keyStr.indexOf(input.charAt(i++));
			enc3 = keyStr.indexOf(input.charAt(i++));
			enc4 = keyStr.indexOf(input.charAt(i++));
			chr1 = (enc1 << 2) | (enc2 >> 4);
			chr2 = ((enc2 & 15) << 4) | (enc3 >> 2);
			chr3 = ((enc3 & 3) << 6) | enc4;
			//output = output + String.fromCharCode(chr1);
			bytes.push(chr1);
			if (enc3 != 64) {
				//output = output + String.fromCharCode(chr2);
				bytes.push(chr2);
			}
			if (enc4 != 64) {
				//output = output + String.fromCharCode(chr3);
				bytes.push(chr3);
			}
			chr1 = chr2 = chr3 = "";
			enc1 = enc2 = enc3 = enc4 = "";

		} while (i < input.length);
		output = JsonDataBinding.bytes2utf8.decode(bytes);
		return unescape(output);
	},
	getPathFromUrl: function(urlStr, webPath) {
		var fn = urlStr;
		if (urlStr && urlStr.length > 4) {
			if (urlStr.indexOf('url(') == 0) {
				fn = urlStr.substr(4);
				if (fn.indexOf(')', fn.length - 1) != -1) {
					fn = fn.substr(0, fn.length - 1);
				}
			}
		}
		if (fn && fn.length > 1) {
			while (fn.indexOf("'") != -1) {
				fn = fn.replace("'", "");
			}
			while (fn.indexOf('"') != -1) {
				fn = fn.replace('"', '');
			}
			if (webPath && webPath.length > 0) {
				if (JsonDataBinding.startsWithI(fn, webPath)) {
					fn = fn.substr(webPath.length);
				}
			}
		}
		return fn;
	},
	getUrlFromPath: function(urlStr, webPath) {
		if (urlStr && urlStr.length > 0) {
			if (urlStr.indexOf('url(') != 0) {
				var fn = urlStr;
				while (fn.indexOf("'") != -1) {
					fn = fn.replace("'", "");
				}
				while (fn.indexOf('"') != -1) {
					fn = fn.replace('"', '');
				}
				if (webPath && webPath.length > 0) {
					if (JsonDataBinding.startsWithI(fn, webPath)) {
						fn = fn.substr(webPath.length);
					}
				}
				fn = 'url(' + fn + ')';
				return fn;
			}
		}
		return urlStr;
	},
	getWindowByPageFilename: function(pageFilename) {
		return _getWindowByPageFilename(pageFilename);
	},
	getWindowById: function(pageId) {
		return _getWindowById(pageId);
	},
	getDocumentById: function(id) {
		if (document.pageId == id) {
			return document;
		}
		if (document.currentDialog && !document.currentDialog.finished) {
			if (document.currentDialog.getPageId() == id) {
				return document.currentDialog.getPageDoc();
			}
		}
		var o = opener;
		while (o) {
			if (o.document.pageId == id) {
				return o.document;
			}
			o = o.opener;
		}
		o = parent;
		while (o) {
			if (o.document.pageId == id) {
				return o.document;
			}
			if (o == o.parent) {
				break;
			}
			o = o.parent;
		}
		var w = _getWindowById(id);
		if (w) {
			return w.document;
		}
	},
	getElementByPageIdId: function(pid, id) {
		var doc = JsonDataBinding.getDocumentById(pid);
		if (doc) {
			return doc.getElementById(id);
		}
	},
	addWindow: function(w) {
		_addWindow(w);
	},
	getParentWindowValue: function(name) {
		if (window.parent != window) {
			return window.parent[name];
		}
	},
	setParentWindowValue: function(name, v) {
		if (window.parent != window) {
			window.parent[name] = v;
		}
	},
	getParentWindowUrl: function(name) {
		if (window.parent != window) {
			return window.parent.document.URL;
		}
		return '';
	},
	isValueTrue: function(v) {
		if (v == null) {
			return false;
		}
		var t = typeof v;
		if (t == 'undefined')
			return false;
		if (t == 'boolean') {
			return v;
		}
		else if (t == 'number') {
			return v != 0;
		}
		else if (t == 'string') {
			if (v.length == 0) {
				return false;
			}
			else if (v == 'none') {
				return false;
			}
			else {
				if ((/^\s*false\s*$/i).test(v)) {
					return false
				}
				else if ((/^\s*0*\s*$/).test(v)) {
					return false
				}
				else if ((/^\s*no\s*$/i).test(v)) {
					return false
				}
				else if ((/^\s*off\s*$/i).test(v)) {
					return false
				}
			}
		}
		return true;
	},
	applyData: function(data, dataAttrs) {
		if (data) {
			_setDataSource.call(data, dataAttrs);
		}
	},
	//===end of data JsonDataBinding api=============================
	//window and page size =======================
	windowTools: {
		scrollBarPadding: 17, // padding to assume for scroll bars
		// center an element in the viewport
		centerElementOnScreen: function(element) {
			this.updateDimensions();
			var left = ((this.pageDimensions.horizontalOffset() + this.pageDimensions.windowWidth() / 2) - (this.scrollBarPadding + element.offsetWidth / 2));
			var top = ((this.pageDimensions.verticalOffset() + this.pageDimensions.windowHeight() / 2) - (this.scrollBarPadding + element.offsetHeight / 2));
			if (left < 0) left = 0;
			if (top < 0) top = 0;
			element.style.top = top + 'px';
			element.style.left = left + 'px';
			element.style.position = 'absolute';
			//if (JsonDataBinding.DebugLevel > 0) {
			//    JsonDataBinding.ShowDebugInfoLine('After centering element. location:(' + element.style.top + ',' + element.style.left + ') size:(' + element.style.width + ',' + element.style.height + ')');
			//}
		},
		// INFORMATION GETTERS
		// load the page size, view port position and vertical scroll offset
		updateDimensions: function() {
			this.updatePageSize();
			this.updateWindowSize();
			this.updateScrollOffset();
		},
		// load page size information
		updatePageSize: function() {
			// document dimensions
			var viewportWidth, viewportHeight;
			if (window.innerHeight && window.scrollMaxY) {
				viewportWidth = document.body.scrollWidth;
				viewportHeight = window.innerHeight + window.scrollMaxY;
			}
			else
				if (document.body.scrollHeight > document.body.offsetHeight) {
				// all but explorer mac
				viewportWidth = document.body.scrollWidth;
				viewportHeight = document.body.scrollHeight;
			}
			else {
				// explorer mac...would also work in explorer 6 strict, mozilla and safari
				viewportWidth = document.body.offsetWidth;
				viewportHeight = document.body.offsetHeight;
			};
			this.pageSize = {
				viewportWidth: viewportWidth,
				viewportHeight: viewportHeight
			};
		},
		// load window size information
		updateWindowSize: function() {
			// view port dimensions
			var windowWidth, windowHeight;
			if (self.innerHeight) {
				// all except explorer
				windowWidth = self.innerWidth;
				windowHeight = self.innerHeight;
			}
			else
				if (document.documentElement && document.documentElement.clientHeight) {
				// explorer 6 strict mode 
				windowWidth = document.documentElement.clientWidth;
				windowHeight = document.documentElement.clientHeight;
			}
			else
				if (document.body) {
				// other explorers
				windowWidth = document.body.clientWidth;
				windowHeight = document.body.clientHeight;
			};
			this.windowSize = {
				windowWidth: windowWidth,
				windowHeight: windowHeight
			};
		},
		// load scroll offset information
		updateScrollOffset: function() {
			// viewport vertical scroll offset
			var horizontalOffset, verticalOffset;
			if (self.pageYOffset) {
				horizontalOffset = self.pageXOffset;
				verticalOffset = self.pageYOffset;
			}
			else
				if (document.documentElement && document.documentElement.scrollTop) {
				// Explorer 6 Strict
				horizontalOffset = document.documentElement.scrollLeft;
				verticalOffset = document.documentElement.scrollTop;
			}
			else if (document.body) {
				// all other Explorers
				horizontalOffset = document.body.scrollLeft;
				verticalOffset = document.body.scrollTop;
			};
			this.scrollOffset = {
				horizontalOffset: horizontalOffset,
				verticalOffset: verticalOffset
			};
		},
		// INFORMATION CONTAINERS
		// raw data containers
		pageSize: {},
		windowSize: {},
		scrollOffset: {},
		// combined dimensions object with bounding logic
		pageDimensions: {
			pageWidth: function() {
				return JsonDataBinding.windowTools.pageSize.viewportWidth > JsonDataBinding.windowTools.windowSize.windowWidth ?
                JsonDataBinding.windowTools.pageSize.viewportWidth :
                JsonDataBinding.windowTools.windowSize.windowWidth;
			},
			pageHeight: function() {
				return JsonDataBinding.windowTools.pageSize.viewportHeight > JsonDataBinding.windowTools.windowSize.windowHeight ?
              JsonDataBinding.windowTools.pageSize.viewportHeight :
                JsonDataBinding.windowTools.windowSize.windowHeight;
			},
			windowWidth: function() {
				return JsonDataBinding.windowTools.windowSize.windowWidth;
			},
			windowHeight: function() {
				return JsonDataBinding.windowTools.windowSize.windowHeight;
			},
			horizontalOffset: function() {
				return JsonDataBinding.windowTools.scrollOffset.horizontalOffset;
			},
			verticalOffset: function() {
				return JsonDataBinding.windowTools.scrollOffset.verticalOffset;
			}
		}
	},
	//end of window and page size
	adjustElementHeight: function (e) {
		if (e && e.children) {
			var h = 0;
			for (var i = 0; i < e.children.length; i++) {
				if (e.children[i]) {
					var t = e.children[i].offsetTop + e.children[i].offsetHeight;
					if (t > h) {
						h = t;
					}
				}
			}
			if (h > 0) {
				e.style.height = (h + 5) + 'px';
			}
		}
	},
	anchorAlign: {
		getElementWidth: function(p, pageSize) {
			if (p == document.body) {
				if (pageSize) {
					return pageSize.w;
				}
				else {
					JsonDataBinding.windowTools.updateDimensions();
					return JsonDataBinding.windowTools.pageDimensions.windowWidth();
				}
			}
			else
				return Math.max(p.offsetWidth, p.scrollWidth);
		},
		getElementHeight: function(p, pageSize) {
			if (p == document.body) {
				if (pageSize) {
					return pageSize.h;
				}
				else {
					JsonDataBinding.windowTools.updateDimensions();
					return JsonDataBinding.windowTools.pageDimensions.windowHeight();
				}
			}
			else
				return Math.max(p.offsetHeight, p.scrollHeight);
		},
		getElementSize: function(p, pageSize) {
			if (p == document.body) {
				if (pageSize) {
					return pageSize;
				}
				else {
					JsonDataBinding.windowTools.updateDimensions();
					return {
						'w': JsonDataBinding.windowTools.pageDimensions.windowWidth(),
						'h': JsonDataBinding.windowTools.pageDimensions.windowHeight()
					};
				}
			}
			else {
				return {
					'w': Math.max(p.offsetWidth, p.scrollWidth),
					'h': Math.max(p.offsetHeight, p.scrollHeight)
				};
			}
		},
		initializeAnchor: function(e, pageSize) {
			for (var i = 0; i < e.childNodes.length; i++) {
				var a = e.childNodes[i];
				if (typeof a != 'undefined' && a != null) {
					if (typeof a.getAttribute != 'undefined') {
						var ah = a.getAttribute('anchor');
						if (typeof ah != 'undefined' && ah != null && ah != '') {
							var ahs = ah.split(',');
							for (var k = 0; k < ahs.length; k++) {
								if (ahs[k] == 'right' || ahs[k] == 'bottom') {
									var psize = JsonDataBinding.anchorAlign.getElementSize(a.parentElement ? a.parentElement : a.parentNode, pageSize);
									a.anchorSize = {
										'x': psize.w - a.offsetLeft - a.offsetWidth,
										'y': psize.h - a.offsetTop - a.offsetHeight
									};
									break;
								}
							}

						}
						JsonDataBinding.anchorAlign.initializeAnchor(a, pageSize);
					}
				}
			}
		},
		initializeBodyAnchor: function() {
			JsonDataBinding.windowTools.updateDimensions();
			var pageSize = {
				'w': JsonDataBinding.windowTools.pageDimensions.windowWidth(),
				'h': JsonDataBinding.windowTools.pageDimensions.windowHeight()
			};
			JsonDataBinding.anchorAlign.initializeAnchor(document.body, pageSize);
		},
		anchorRight: function(e, pageSize) {
			if (e.anchorSize) {
				var p = e.parentElement ? e.parentElement : e.parentNode;

				var pw = JsonDataBinding.anchorAlign.getElementWidth(p, pageSize);
				var x = pw - e.anchorSize.x - e.offsetWidth;
				if (x < 0) x = 0;
				e.style.left = x + 'px';
			}
		},
		anchorBottom: function(e, pageSize) {
			if (e.anchorSize) {
				var p = e.parentElement ? e.parentElement : e.parentNode;

				var ph = JsonDataBinding.anchorAlign.getElementHeight(p, pageSize);
				var y = ph - e.anchorSize.y - e.offsetHeight;
				if (y < 0) y = 0;
				e.style.top = y + "px";
			}
		},
		anchorLeftRight: function(e, pageSize) {
			if (e.anchorSize) {
				var p = e.parentElement ? e.parentElement : e.parentNode;

				var pw = JsonDataBinding.anchorAlign.getElementWidth(p, pageSize);
				var w = pw - e.anchorSize.x - e.offsetLeft;
				if (w > 0) {
					e.style.width = w + "px";
				}
			}
		},
		anchorTopBottom: function(e, pageSize) {
			if (e.anchorSize) {
				var p = e.parentElement ? e.parentElement : e.parentNode;

				var ph = JsonDataBinding.anchorAlign.getElementHeight(p, pageSize);
				var h = ph - e.anchorSize.y - e.offsetTop;
				if (h > 0) {
					e.style.height = h + "px";
				}
			}
		},
		alignCenterElement: function(e, pageSize) {
			var p = e.parentElement ? e.parentElement : e.parentNode;
			var ps = JsonDataBinding.anchorAlign.getElementSize(p, pageSize);
			var w = e.offsetWidth;
			var h = e.offsetHeight;
			//
			var x = (ps.w - w) / 2;
			var y = (ps.h - h) / 2;
			if (x < 0) x = 0;
			if (y < 0) y = 0;
			e.style.left = x + 'px';
			e.style.top = y + 'px';
		},
		alignTopCenterElement: function(e, pageSize) {
			var p = e.parentElement ? e.parentElement : e.parentNode;

			var pw = JsonDataBinding.anchorAlign.getElementWidth(p, pageSize);
			var w = e.offsetWidth;
			//
			var x = (pw - w) / 2;
			if (x < 0) x = 0;
			e.style.left = x + 'px';
		},
		alignBottomCenterElement: function(e, pageSize) {
			var p = e.parentElement ? e.parentElement : e.parentNode;

			var ps = JsonDataBinding.anchorAlign.getElementSize(p, pageSize);
			var h = e.offsetHeight;
			var w = e.offsetWidth;
			//
			var y = ps.h - h;
			var x = (ps.w - w) / 2;
			if (x < 0) x = 0;
			if (y < 0) y = 0;
			e.style.left = x + 'px';
			e.style.top = y + 'px';
		},
		alignTopRightElement: function(e, pageSize) {
			var p = e.parentElement ? e.parentElement : e.parentNode;
			var pw = JsonDataBinding.anchorAlign.getElementWidth(p, pageSize);
			var w = e.offsetWidth;
			//
			var x = (pw - w);
			if (x < 0) x = 0;
			e.style.left = x + 'px';
		},
		alignLeftCenterElement: function(e, pageSize) {
			var p = e.parentElement ? e.parentElement : e.parentNode;
			var ph = JsonDataBinding.anchorAlign.getElementHeight(p, pageSize);
			var h = e.offsetHeight;
			//
			var y = (ph - h) / 2;
			if (y < 0) y = 0;
			e.style.top = y + "px";
		},
		alignLeftBottomElement: function(e, pageSize) {
			var p = e.parentElement ? e.parentElement : e.parentNode;
			var ph = JsonDataBinding.anchorAlign.getElementHeight(p, pageSize);
			var h = e.offsetHeight;
			//
			var y = (ph - h);
			if (y < 0) y = 0;
			e.style.top = y + "px";
		},

		alignCenterRightElement: function(e, pageSize) {
			var p = e.parentElement ? e.parentElement : e.parentNode;

			var ps = JsonDataBinding.anchorAlign.getElementSize(p, pageSize);
			var w = e.offsetWidth;
			var h = e.offsetHeight;
			//
			var x = (ps.w - w);
			var y = (ps.h - h) / 2;
			if (x < 0) x = 0;
			if (y < 0) y = 0;
			e.style.top = y + 'px';
			e.style.left = x + 'px';
		},
		alignBottomRightElement: function(e, pageSize) {
			var p = e.parentElement ? e.parentElement : e.parentNode;

			var ps = JsonDataBinding.anchorAlign.getElementSize(p, pageSize);
			var es = JsonDataBinding.anchorAlign.getElementSize(e, pageSize);
			//
			var x = (ps.w - es.w) - 2;
			var y = (ps.h - es.h) - 2;
			if (x < 0) x = 0;
			if (y < 0) y = 0;
			e.style.left = x + 'px';
			e.style.top = y + 'px';

		},
		applyBodyAnchorAlign: function() {
			JsonDataBinding.windowTools.updateDimensions();
			var pageSize = {
				'w': JsonDataBinding.windowTools.pageDimensions.windowWidth(),
				'h': JsonDataBinding.windowTools.pageDimensions.windowHeight()
			};
			JsonDataBinding.anchorAlign.applyAnchorAlign(document.body, pageSize);
		},
		applyAnchorAlign: function(e, pageSize) {
			if (!e) return;
			for (var i = 0; i < e.childNodes.length; i++) {
				var a = e.childNodes[i];
				if (typeof a != 'undefined' && a != null) {
					if (typeof a.getAttribute != 'undefined') {
						var ah = a.getAttribute('anchor');
						if (typeof ah != 'undefined' && ah != null && ah != '') {
							var ahs = ah.split(',');
							var ahLeft = false;
							var ahRight = false;
							var ahTop = false;
							var ahBottom = false;
							var posAlign = '';
							for (var k = 0; k < ahs.length; k++) {
								if (ahs[k] == 'right') ahRight = true;
								else if (ahs[k] == 'bottom') ahBottom = true;
								else if (ahs[k] == 'left') ahLeft = true;
								else if (ahs[k] == 'top') ahTop = true;
								else posAlign = ahs[k];
							}
							var bAdjusted = false;
							if (ahRight || ahBottom) {
								if (ahRight) {
									if (ahLeft)
										JsonDataBinding.anchorAlign.anchorLeftRight(a, pageSize);
									else
										JsonDataBinding.anchorAlign.anchorRight(a, pageSize);
									bAdjusted = true;
								}
								if (ahBottom) {
									if (ahTop)
										JsonDataBinding.anchorAlign.anchorTopBottom(a, pageSize);
									else
										JsonDataBinding.anchorAlign.anchorBottom(a, pageSize);
									bAdjusted = true;
								}
								if ((ahRight && ahLeft) || (ahBottom && ahTop)) {
									JsonDataBinding.anchorAlign.applyAnchorAlign(a, pageSize);
									bAdjusted = true;
								}
							}
							else {
								bAdjusted = true;
								if (posAlign == 'center')
									JsonDataBinding.anchorAlign.alignCenterElement(a, pageSize);
								else if (posAlign == 'topcenter')
									JsonDataBinding.anchorAlign.alignTopCenterElement(a, pageSize);
								else if (posAlign == 'topright')
									JsonDataBinding.anchorAlign.alignTopRightElement(a, pageSize);
								else if (posAlign == 'leftcenter')
									JsonDataBinding.anchorAlign.alignLeftCenterElement(a, pageSize);
								else if (posAlign == 'leftbottom')
									JsonDataBinding.anchorAlign.alignLeftBottomElement(a, pageSize);
								else if (posAlign == 'bottomcenter')
									JsonDataBinding.anchorAlign.alignBottomCenterElement(a, pageSize);
								else if (posAlign == 'centerright')
									JsonDataBinding.anchorAlign.alignCenterRightElement(a, pageSize);
								else if (posAlign == 'bottomright')
									JsonDataBinding.anchorAlign.alignBottomRightElement(a, pageSize);
								else
									bAdjusted = false;
							}
							if (bAdjusted) {
								function refreshDataRepeater(e0) {
									if (e0 && e0.children) {
										for (var i = 0; i < e0.children.length; i++) {
											if (e0.children[i].IsDataRepeater && e0.children[i].jsData) {
												e0.children[i].jsData.refreshPage();
											}
											else {
												refreshDataRepeater(e0.children[i]);
											}
										}
									}
								}
								refreshDataRepeater(a);
								if (a.onAdjustAnchorAlign) {
									a.onAdjustAnchorAlign();
								}
							}
						}
					}
				}
			}
		}

	},
	//end of anchorAlign
	//===element position ===================================================
	ElementPosition: {
		_elementPos: function() {
			function __getIEVersion() {
				var rv = -1; // Return value assumes failure.
				if (navigator.appName == 'Microsoft Internet Explorer') {
					var ua = navigator.userAgent;
					var re = new RegExp("MSIE ([0-9]{1,}[\.0-9]{0,})");
					if (re.exec(ua) != null)
						rv = parseFloat(RegExp.$1);
				}
				return rv;
			}

			function __getOperaVersion() {
				var rv = 0; // Default value
				if (window.opera) {
					var sver = window.opera.version();
					rv = parseFloat(sver);
				}
				return rv;
			}

			var __userAgent = navigator.userAgent;
			var __isIE = navigator.appVersion.match(/MSIE/) != null;
			var __IEVersion = __getIEVersion();
			var __isIENew = __isIE && __IEVersion >= 8;
			var __isIEOld = __isIE && !__isIENew;

			var __isFireFox = __userAgent.match(/firefox/i) != null;
			var __isFireFoxOld = __isFireFox && ((__userAgent.match(/firefox\/2./i) != null) || (__userAgent.match(/firefox\/1./i) != null));
			var __isFireFoxNew = __isFireFox && !__isFireFoxOld;

			var __isWebKit = navigator.appVersion.match(/WebKit/) != null;
			var __isChrome = navigator.appVersion.match(/Chrome/) != null;
			var __isOpera = window.opera != null;
			var __operaVersion = __getOperaVersion();
			var __isOperaOld = __isOpera && (__operaVersion < 10);

			function __parseBorderWidth(width) {
				var res = 0;
				if (typeof (width) == "string" && width != null && width != "") {
					var p = width.indexOf("px");
					if (p >= 0) {
						res = parseInt(width.substring(0, p));
					}
					else {
						//do not know how to calculate other values (such as 0.5em or 0.1cm) correctly now
						//so just set the width to 1 pixel
						res = 1;
					}
				}
				return res;
			}


			//returns border width for some element
			function __getBorderWidth(element) {
				var res = new Object();
				res.left = 0; res.top = 0; res.right = 0; res.bottom = 0;
				if (window.getComputedStyle) {
					//for Firefox
					var elStyle = window.getComputedStyle(element, null);
					res.left = parseInt(elStyle.borderLeftWidth.slice(0, -2));
					res.top = parseInt(elStyle.borderTopWidth.slice(0, -2));
					res.right = parseInt(elStyle.borderRightWidth.slice(0, -2));
					res.bottom = parseInt(elStyle.borderBottomWidth.slice(0, -2));
				}
				else {
					//for other browsers
					res.left = __parseBorderWidth(element.style.borderLeftWidth);
					res.top = __parseBorderWidth(element.style.borderTopWidth);
					res.right = __parseBorderWidth(element.style.borderRightWidth);
					res.bottom = __parseBorderWidth(element.style.borderBottomWidth);
				}

				return res;
			}

			//returns the absolute position of some element within document
			getElementAbsolutePos = function(element) {
				var res = new Object();
				res.x = 0; res.y = 0;
				if (element !== null) {
					try {
						if (element.getBoundingClientRect) {
							var viewportElement;
							if (IsSafari() || IsChrome()) {
								viewportElement = document.body;
							}
							else {
								viewportElement = document.documentElement;
							}
							var box = element.getBoundingClientRect();
							var scrollLeft = viewportElement.scrollLeft;
							var scrollTop = viewportElement.scrollTop;
							res.x = box.left + scrollLeft;
							res.y = box.top + scrollTop;
						}
						else { //for old browsers
							res.x = element.offsetLeft;
							res.y = element.offsetTop;
							var parentNode = element.parentNode;
							var borderWidth = null;
							while (offsetParent != null) {
								res.x += offsetParent.offsetLeft;
								res.y += offsetParent.offsetTop;
								var parentTagName = offsetParent.tagName.toLowerCase();

								if ((__isIEOld && parentTagName != "table") || ((__isFireFoxNew || __isChrome) && parentTagName == "td")) {
									borderWidth = kGetBorderWidth(offsetParent);
									res.x += borderWidth.left;
									res.y += borderWidth.top;
								}
								if (offsetParent != document.body && offsetParent != document.documentElement) {
									res.x -= offsetParent.scrollLeft;
									res.y -= offsetParent.scrollTop;
								}
								//next lines are necessary to fix the problem with offsetParent
								if (!__isIE && !__isOperaOld || __isIENew) {
									while (offsetParent != parentNode && parentNode !== null) {
										res.x -= parentNode.scrollLeft;
										res.y -= parentNode.scrollTop;
										if (__isFireFoxOld || __isWebKit) {
											borderWidth = kGetBorderWidth(parentNode);
											res.x += borderWidth.left;
											res.y += borderWidth.top;
										}
										parentNode = parentNode.parentNode;
									}
								}
								parentNode = offsetParent.parentNode;
								offsetParent = offsetParent.offsetParent;
							}
						}
					}
					catch (err) {
						if (element.parentNode) {
							return getElementAbsolutePos(element.parentNode);
						}
					}
				}
				return res;
			}
		} (),
		getElementPosition: function(element) {
			return getElementAbsolutePos(element);
		}
	},
	//===end of ElementPosition =======================
	//===text box monitor ===========================================
	_textBoxObserver: function() {
		var textBoxes;
		var timerId;
		var poll = function() {
			if (!textBoxes) return;
			for (var i = 0; i < textBoxes.length; i++) {
				var ctrl = textBoxes[i];
				if (!ctrl.disableMonitor) {
					var changed = false;
					if (ctrl.isCheckBox) {
						if (ctrl.val != ctrl.checked) {
							if (ctrl.val == null) {
								if (ctrl.checked != null && ctrl.checked != 'null') {
									changed = true;
								}
							}
							else {
								changed = true;
							}
						}
					}
					else if (ctrl.isHtml) {
						if (ctrl.val != ctrl.innerHTML) {
							if (ctrl.val == null) {
								if (ctrl.innerHTML != null && ctrl.innerHTML != 'null') {
									changed = true;
								}
							}
							else {
								changed = true;
							}
						}
					}
					else if (ctrl.isInnerText) {
						var txt = JsonDataBinding.GetInnerText(ctrl);
						if (ctrl.val != txt) {
							if (ctrl.val == null) {
								if (txt != null && txt != 'null') {
									changed = true;
								}
							}
							else {
								changed = true;
							}
						}
					}
					else {
						if (ctrl.val != ctrl.value) {
							if (ctrl.val == null) {
								if (ctrl.nullDisplayEmpty) {
									if (ctrl.value != null && ctrl.value != JsonDataBinding.NullDisplay) {
										changed = true;
									}
								}
								else if (ctrl.value != null && ctrl.value != 'null') {
									changed = true;
								}
							}
							else {
								changed = true;
							}
						}
					}
					if (changed) {
						var evt;
						if (ctrl.isCheckBox) {
							ctrl.val = ctrl.checked;
							if (ctrl.onCheckedChanged) {
								ctrl.onCheckedChanged(ctrl);
							}
						}
						else if (ctrl.isHtml) {
							ctrl.val = ctrl.innerHTML;
							if (ctrl.oninnerHtmlChanged) {
								ctrl.oninnerHtmlChanged(ctrl);
							}
						}
						else if (ctrl.isInnerText) {
							ctrl.val = JsonDataBinding.GetInnerText(ctrl);
							if (ctrl.onsetbounddata) {
								ctrl.onsetbounddata(ctrl);
							}
							if (ctrl.ontxtChange) {
								ctrl.ontxtChange({ target: ctrl });
							}
							else {
								JsonDataBinding.fireEvent(ctrl, 'change');
							}
							/*
							if (ctrl.fireEvent) {
							ctrl.fireEvent("onchange"); // for IE 
							}
							else if (document.createEvent && ctrl.dispatchEvent) {
							evt = document.createEvent("HTMLEvents");
							evt.initEvent("change", true, true);
							ctrl.dispatchEvent(evt); // for DOM-compliant browsers
							}
							*/
						}
						else {
							ctrl.val = ctrl.value;
							if (ctrl.onsetbounddata) {
								ctrl.onsetbounddata(ctrl);
							}
							if (ctrl.ontxtChange) {
								ctrl.ontxtChange({ target: ctrl });
							}
							else {
								JsonDataBinding.fireEvent(ctrl, 'change');
							}
							/*
							if (ctrl.fireEvent) {
							ctrl.fireEvent("onchange"); // for IE
							}
							else if (document.createEvent && ctrl.dispatchEvent) {
							evt = document.createEvent("HTMLEvents");
							evt.initEvent("change", true, true);
							ctrl.dispatchEvent(evt); // for DOM-compliant browsers
							}
							*/
						}
					}
				}
			}
		}
		AddTextBox = function(textBox) {
			if (typeof textBoxes == 'undefined') {
				textBoxes = new Array();
			}
			var found = false;
			for (var i = 0; i < textBoxes.length; i++) {
				if (textBoxes[i] == textBox) {
					found = true;
					break;
				}
			}
			if (!found) {
				if (textBox.isCheckBox) {
					textBox.val = textBox.checked;
				}
				else {
					var tag = textBox.tagName.toLowerCase();
					if (tag == 'div') {
						textBox.val = textBox.innerHTML;
						textBox.isHtml = true;
					}
					//                    else if (tag == 'textarea') {
					//                        textBox.val = JsonDataBinding.GetInnerText(textBox);
					//                        textBox.isInnerText = true;
					//                        textBox.isHtml = false;
					//                    }
					else {
						textBox.val = textBox.value;
						textBox.isHtml = false;
						textBox.isInnerText = false;
					}
				}
				textBoxes.push(textBox);
			}
			if (typeof timerId == 'undefined') {
				timerId = window.setInterval(poll, 300);
			}
		}
		ShowTextBoxCount = function() {
			if (typeof textBoxes == 'undefined')
				return 0;
			return textBoxes.length;
		}
		_pollModifications = function() {
			poll();
		}
	} (),
	// Text box watcher API ===========================
	addTextBoxObserver: function(textBox) {
		AddTextBox(textBox);
	},
	pollModifications: function() {
		_pollModifications();
	},
	//end of Text box watcher ==========================
	//html table ======================================
	//tableElement: html element
	//jsTable: json data table
	HtmlTableData: function(tableElement, jsTable) {
		var _tblElement = tableElement;
		var _jsonTable = jsTable;
		var _readOnly = false;
		var _rowTemplate;
		var _actCtrls;
		var _selectedRow;
		var _textBoxElement;
		var _buttonElement;
		var _selectionElement;
		var _datetimePickerButton;
		var _lookupTableElements;
		var _chklstTableElements;
		var _onresizeAttached = false;
		//
		var EDITOR_NONE = -1;
		var EDITOR_TEXT = 0;
		var EDITOR_ENUM = 1;
		var EDITOR_DATETIME = 2;
		var EDITOR_DBLOOKUP = 3;
		var EDITOR_CHKLIST = 4;
		//
		var attr = _tblElement.getAttribute('readonly');
		if (typeof attr != 'undefined' && attr) {
			_readOnly = true;
		}
		var tbody = JsonDataBinding.getTableBody(_tblElement);
		var k;
		if (tbody.rows.length > 0) {
			_rowTemplate = new Array();
			for (k = 0; k < tbody.rows[0].cells.length; k++) {
				_rowTemplate[k] = tbody.rows[0].cells[k].style.cssText;
			}
		}
		else {
			var th = getHeader();
			if (th.rows && th.rows.length > 0) {
				_rowTemplate = new Array();
				for (k = 0; k < th.rows[0].cells.length; k++) {
					_rowTemplate[k] = th.rows[0].cells[k].style.cssText;
				}
			}
		}
		if (_tblElement.ActControls) {
			_actCtrls = new Array();
			for (k = 0; k < _tblElement.ActControls.length; k++) {
				var ac = document.getElementById(_tblElement.ActControls[k]);
				if (ac) {
					_actCtrls.push(ac);
				}
			}
		}
		function showActCtrls(cells) {
			if (_actCtrls && _actCtrls.length > 0) {
				var tdAct;
				if (cells.length <= _jsonTable.Columns.length) {
					//action cell is created elsewhere
				}
				else {
					tdAct = cells[_jsonTable.Columns.length];
				}
				if (tdAct) {
					if (_tblElement.ActColWidth) {
						tdAct.style.width = _tblElement.ActColWidth + 'px';
					}
					for (c = 0; c < _actCtrls.length; c++) {
						_actCtrls[c].style.position = 'static';
						_actCtrls[c].style.left = 'auto';
						_actCtrls[c].style.top = 'auto';
						tdAct.appendChild(_actCtrls[c]);
						_actCtrls[c].style.display = 'inline';
					}
				}
			}
		}
		function init() {
			//link to other editing controls, i.e. text box, text area, etc.
			JsonDataBinding.addvaluechangehandler(_jsonTable.TableName, _tblElement);
			JsonDataBinding.AttachOnRowDeleteHandler(_jsonTable.TableName, onrowdelete);
			if (_tblElement.ReadOnlyFields) {
				if (_tblElement.ReadOnlyFields.length > 0) {
					for (var i = 0; i < _tblElement.ReadOnlyFields.length; i++) {
						var cn = _tblElement.ReadOnlyFields[i].toLowerCase();
						for (var c = 0; c < _jsonTable.Columns.length; c++) {
							if (cn == _jsonTable.Columns[c].Name.toLowerCase()) {
								_jsonTable.Columns[c].ReadOnly = true;
								break;
							}
						}
					}
				}
			}
			recreateTableElement();
		}
		if (_tblElement.FieldEditors) {
			for (var c = 0; c < _jsonTable.Columns.length; c++) {
				var ed = _tblElement.FieldEditors[c];
				if (ed) {
					if (ed.Editor == EDITOR_CHKLIST) {
						var tblList = JsonDataBinding.createCheckedList(ed.Tablename, _tblElement, c);
						if (!_chklstTableElements) {
							_chklstTableElements = {};
						}
						_chklstTableElements[ed.Tablename] = tblList;
						tblList.chklist.setPosition("absolute");
						ed.CellPainter = tblList.chklist;
					}
				}
			}
		}
		//
		function getCellLocation(element) {
			return JsonDataBinding.ElementPosition.getElementPosition(element);
		}
		function getCell(e) {
			var c = getEventSender(e);
			if (c) {
				if (c.tr) {
					return c;
				}
				if (c.ownerCell) {
					return c.ownerCell;
				}
			}
			return null;
		}
		function createCell(c) {
			var td = document.createElement('TD');
			if (_rowTemplate) {
				if (_rowTemplate.length > c && _rowTemplate[c] && _rowTemplate[c].length > 0) {
					td.style.cssText = _rowTemplate[c];
				}
			}
			if (_tblElement) {
				if (_tblElement.ColumnAligns && _tblElement.ColumnAligns[c]) {
					td.align = _tblElement.ColumnAligns[c];
				}
				if (_tblElement.ColumnWidths && _tblElement.ColumnWidths[c]) {
					td.style.width = _tblElement.ColumnWidths[c];
				}
				if (_tblElement.InvisibleColumns && _tblElement.InvisibleColumns[c]) {
					td.style.display = 'none';
				}
			}
			return td;
		}
		function showCell(td, c, val0) {
			var editor;
			var isBlob = false;
			var val = JsonDataBinding.toText(val0);
			if (_jsonTable.Columns.length > c) {
				isBlob = (_jsonTable.Columns[c].Type == 252);
			}
			var isImage = false;
			var isImages = JsonDataBinding.getObjectProperty(_jsonTable.TableName, 'IsFieldImage');
			if (isImages && isImages.length > c) {
				isImage = isImages[c];
			}
			if (_tblElement.FieldEditors && _tblElement.FieldEditors[c]) {
				editor = _tblElement.FieldEditors[c];
			}
			if (editor && editor.Editor == EDITOR_ENUM) {
				var found = false;
				if (editor.Values) {
					for (var i = 0; i < editor.Values.length; i++) {
						if (editor.Values[i][1] == val) {
							found = true;
							var txt = document.createTextNode(editor.Values[i][0]);
							td.appendChild(txt);
							break;
						}
					}
				}
				if (!found) {
					var txt2 = document.createTextNode(val);
					td.appendChild(txt2);
				}
			}
			else if (editor && editor.CellPainter) {
				td.showCell = editor.CellPainter.showCell(td, c, val);
			}
			else {
				if (typeof val == 'undefined' || val == null) {
					td.appendChild(document.createTextNode(JsonDataBinding.NullDisplayText));
				}
				else {
					if (isImage) {
						var img = td.getElementsByTagName('img');
						if (img && img.length > 0) {
							img = img[0];
						}
						else {
							img = document.createElement('img');
							td.appendChild(img);
						}
						if (isBlob) {
							img.src = 'data:image/jpg;base64,' + val;
						}
						else {
							img.src = val;
						}
					}
					else {
						if (_tblElement.ColumnAsHTML && _tblElement.ColumnAsHTML[c]) {
							td.innerHTML = val;
						}
						else {
							var txt3 = document.createTextNode(val);
							td.appendChild(txt3);
						}
					}
				}
			}
		}
		function addHtmlTableRow(tbody, r) {
			var isDeleted = (_jsonTable.Rows[r].deleted || _jsonTable.Rows[r].removed);
			var tr = tbody.insertRow(-1);
			tr.datarownum = r;
			for (var c = 0; c < _jsonTable.Columns.length; c++) {
				var td = createCell(c);
				JsonDataBinding.AttachEvent(td, 'onmouseover', onCellMouseOver);
				JsonDataBinding.AttachEvent(td, 'onmouseout', onCellMouseOut);
				JsonDataBinding.AttachEvent(td, 'onclick', onCellClick);
				showCell(td, c, _jsonTable.Rows[r].ItemArray[c]);
				td.datarownum = r;
				td.columnIndex = c;
				td.tr = tr;
				tr.appendChild(td);
			}
			if (_actCtrls && _actCtrls.length > 0) {
				var tdAct = document.createElement('td');
				tdAct.datarownum = r;
				JsonDataBinding.AttachEvent(tdAct, 'onmouseover', onCellMouseOver);
				JsonDataBinding.AttachEvent(tdAct, 'onmouseout', onCellMouseOut);
				JsonDataBinding.AttachEvent(tdAct, 'onclick', onCellClick);
				tdAct.tr = tr;
				tr.appendChild(tdAct);
			}
			if (isDeleted) {
				tr.style.display = 'none';
			}
			else {
				tr.style.display = '';
			}
			if (_jsonTable.rowIndex == r) {
				_selectedRow = tr;
			}
			if (_tblElement.AlternateBackgroundColor && r % 2 != 0) {
				tr.style.backgroundColor = _tblElement.AlternateBackgroundColor;
			}
		}
		function getHeader() {
			var th = _tblElement.tHead;
			if (typeof th == 'undefined' || th == null) {
				th = document.createElement('thead');
				_tblElement.appendChild(th);
			}
			return th;
		}
		var _tblCreated;
		function recreateTableElement() {
			_selectedRow = null;
			var tbody = JsonDataBinding.getTableBody(_tblElement);
			if (tbody == null) {
				tbody = document.createElement('tbody');
				_tblElement.appendChild(tbody);
			}
			tbody.isJs = true;
			while (tbody.rows.length > 0) {
				tbody.deleteRow(tbody.rows.length - 1);
			}
			var th = getHeader();
			var tr;
			if (th.rows.length > 0) {
				tr = th.rows[0];
			}
			else {
				tr = document.createElement('tr');
				th.appendChild(tr);
			}
			var c;
			for (c = 0; c < _jsonTable.Columns.length; c++) {
				if (c < tr.cells.length) {
					if (tr.cells[c].innerHTML.length == 0) {
						JsonDataBinding.SetInnerText(tr.cells[c], _jsonTable.Columns[c].Name);
					}
				}
				else {
					var td = document.createElement('td');
					td.appendChild(document.createTextNode(_jsonTable.Columns[c].Name));
					tr.appendChild(td);
				}
				if (_tblElement && _tblElement.InvisibleColumns && _tblElement.InvisibleColumns[c]) {
					tr.cells[c].style.display = 'none';
				}
			}
			for (var r = 0; r < _jsonTable.Rows.length; r++) {
				addHtmlTableRow(tbody, r);
			}
			if (_selectedRow != null) {
				var cells = _selectedRow.getElementsByTagName("td");
				showActCtrls(cells);
				if (_tblElement.SelectedRowColor) {
					for (c = 0; c < cells.length; c++) {
						cells[c].style.backgroundColor = _tblElement.SelectedRowColor;
					}
				}
			}
			if (/*!_tblElement.UseFixedColWidths &&*/ _tblElement.ShowVerticalScrollbar) {
				tbody.style.display = 'block';
				tbody.style.overflowY = 'auto';//'scroll';
				_tblElement.style.borderCollapse = 'separate';
				_tblElement.tableLayout = 'fixed';
				var desiredHeight;
				if (_tblElement.VerticalHeight > 1) {
					desiredHeight = _tblElement.VerticalHeight;
				}
				else {
					desiredHeight = Math.round(_tblElement.parentNode.clientHeight * _tblElement.VerticalHeight);
				}
				function _adjustHeaderWidthes() {
					//if (!_tblElement.UseFixedColWidths) {
					if (th.clientHeight > 0) {
						th.style.display = 'block';
						if (desiredHeight > th.clientHeight)
							tbody.style.height = (desiredHeight - th.clientHeight) + 'px';
						else
							tbody.style.height = '1px';
						if (_jsonTable.Rows.length > 0) {
							var r0 = tbody.rows[0];
							for (c = 0; c < _jsonTable.Columns.length - 1; c++) {
								if (_tblElement && _tblElement.InvisibleColumns && _tblElement.InvisibleColumns[c]) {
								}
								else {
									if (tr.cells[c].clientWidth > r0.cells[c].clientWidth) {
										r0.cells[c].style.width = tr.cells[c].clientWidth + 'px';
									}
									else if (tr.cells[c].clientWidth < r0.cells[c].clientWidth) {
										tr.cells[c].style.width = r0.cells[c].clientWidth + 'px';
									}
								}
							}
						}
					}
					else {
						tbody.style.height = desiredHeight + 'px';
					}
					//}
				}
				_adjustHeaderWidthes();
				_tblCreated = {
					adjustColumnWidthes: function () {
						_adjustHeaderWidthes();
					}
				};
			}
		}
		function _adjustColumnWidthes() {
			if (_tblCreated) {
				_tblCreated.adjustColumnWidthes();
			}
		}
		function _onDataReady(jsTable) {
			_jsonTable = jsTable;
			init();
		}
		function onCellMouseOver(e) {
			var tbody = JsonDataBinding.getTableBody(_tblElement);
			if (!tbody)
				return;
			var cell = getCell(e);
			if (cell) {
				var cells = cell.tr.getElementsByTagName("td");
				var bkc = tbody.style.backgroundColor;
				if (_tblElement.HighlightRowColor) {
					bkc = _tblElement.HighlightRowColor;
				}
				for (var c = 0; c < cells.length; c++) {
					cells[c].style.backgroundColor = bkc;
				}
				if (_tblElement.HighlightCellColor) {
					cell.style.backgroundColor = _tblElement.HighlightCellColor;
				}
				cell.cellLocation = getCellLocation(cell);
			}
		}

		function onCellMouseOut(e) {
			var tbody = JsonDataBinding.getTableBody(_tblElement);
			if (!tbody)
				return;
			var cell = getCell(e);
			if (cell) {
				var cells = cell.tr.getElementsByTagName("td");
				var bkC;
				if (typeof _jsonTable.rowIndex != 'undefined' && cell.datarownum == _jsonTable.rowIndex) {
					if (_tblElement.SelectedRowColor) {
						bkC = _tblElement.SelectedRowColor;
					}
					else {
						bkC = tbody.style.backgroundColor;
					}
				}
				else {
					if (_tblElement.AlternateBackgroundColor && cell.datarownum % 2 != 0) {
						bkC = _tblElement.AlternateBackgroundColor;
					}
					else {
						bkC = tbody.style.backgroundColor;
					}
				}
				for (var c = 0; c < cells.length; c++) {
					cells[c].style.backgroundColor = bkC;
				}
			}
		}
		//change by text box
		function ontextboxvaluechange() {
			if (typeof _textBoxElement != 'undefined') {
				if (!_textBoxElement.disableMonitor) {
					if (typeof _textBoxElement.cell != 'undefined' && _textBoxElement.cell != null) {
						if (JsonDataBinding.GetInnerText(_textBoxElement.cell) != _textBoxElement.value) {
							_backupdata(_jsonTable.TableName, _textBoxElement.cell.datarownum);
							JsonDataBinding.SetInnerText(_textBoxElement.cell, _textBoxElement.value);
							_jsonTable.Rows[_textBoxElement.cell.datarownum].changed = true;
							_jsonTable.Rows[_textBoxElement.cell.datarownum].ItemArray[_textBoxElement.cell.columnIndex] = _textBoxElement.value;
							JsonDataBinding.onvaluechanged(_jsonTable, _textBoxElement.cell.datarownum, _textBoxElement.cell.columnIndex, _textBoxElement.value);
							if (_tblElement.ColumnValueChanged) {
								_tblElement.ColumnValueChanged(_tblElement, _textBoxElement.cell.datarownum, _textBoxElement.cell.columnIndex, _textBoxElement.value);
							}
						}
					}
				}
			}
		}
		function oncellbuttonclick() {
			if (_tblElement.ReadOnly) return;
			var zi;
			if (_buttonElement) {
				if (_buttonElement.cell) {
					var cell = _buttonElement.cell;
					if (_buttonElement.editor == EDITOR_ENUM) {
						var needFill = true;
						if (typeof _selectionElement != 'undefined' && _selectionElement.cell && _selectionElement.cell.columnIndex == _buttonElement.cell.columnIndex) {
							needFill = false;
						}
						if (needFill) {
							var newSel = document.createElement('select');
							newSel.style.position = 'absolute';
							zi = parseInt(_tblElement.style.zIndex ? _tblElement.style.zIndex : 0) + 2;
							newSel.style.zIndex = zi;
							if (typeof _selectionElement != 'undefined') {
								document.body.replaceChild(newSel, _selectionElement);
							}
							else {
								document.body.appendChild(newSel);
							}
							for (var i = 0; i < _tblElement.FieldEditors[cell.columnIndex].Values.length; i++) {
								var elOptNew = document.createElement('option');
								elOptNew.text = _tblElement.FieldEditors[cell.columnIndex].Values[i][0];
								elOptNew.value = _tblElement.FieldEditors[cell.columnIndex].Values[i][1];
								try {
									newSel.add(elOptNew, null); // standards compliant; doesn't work in IE
								}
								catch (ex) {
									newSel.add(elOptNew); // IE only
								}
							}
							_selectionElement = newSel;
							JsonDataBinding.AttachEvent(_selectionElement, 'onclick', oncellselectionclick);
							JsonDataBinding.AttachEvent(_selectionElement, 'onkeydown', oncellselectionkeydown);
						}
						_selectionElement.cell = _buttonElement.cell;
						_selectionElement.style.left = (_buttonElement.cell.cellLocation.x + document.body.scrollLeft) + 'px';
						_selectionElement.style.top = (_buttonElement.offsetTop + _buttonElement.offsetHeight) + 'px';
						_selectionElement.style.display = 'block';
						//
						_selectionElement.size = _selectionElement.options.length;
						JsonDataBinding.windowTools.updateDimensions();
						if (_selectionElement.offsetHeight + _selectionElement.offsetTop > JsonDataBinding.windowTools.pageDimensions.windowHeight()) {
							var newTop = _buttonElement.offsetTop - _selectionElement.offsetHeight;
							if (newTop >= 0) {
								_selectionElement.style.top = newTop + "px";
							}
						}
						zi = JsonDataBinding.getPageZIndex(_selectionElement) + 1;
						_selectionElement.style.zIndex = zi;
						_selectionElement.focus();
					}
					else if (_buttonElement.editor == EDITOR_DBLOOKUP) {
						if (!_lookupTableElements) {
							_lookupTableElements = {};
						}
						if (_buttonElement.cell) {
							if (_tblElement.FieldEditors) {
								if (_tblElement.FieldEditors[_buttonElement.cell.columnIndex]) {
									var tname = _tblElement.FieldEditors[_buttonElement.cell.columnIndex].TableName;
									var tbl = _lookupTableElements[tname];
									if (!tbl) {
										var u = new Object();
										tbl = document.createElement("table");
										tbl.setAttribute("jsdb", tname);
										tbl.style.position = 'absolute';
										zi = 1 + parseInt((_tblElement.style.zIndex) ? _tblElement.style.zIndex : 1);
										tbl.style.zIndex = zi;
										tbl.border = 1;
										tbl.id = tname;
										tbl.TargetTable = _tblElement;
										tbl.Editor = _tblElement.FieldEditors[_buttonElement.cell.columnIndex];
										tbl.style.backgroundColor = 'white';
										tbl.HighlightRowColor = '#c0ffc0';
										tbl.SelectedRowColor = '#c0c0ff';
										tbl.setAttribute('readonly', 'true');
										var _onmousedown = function(e) {
											var target = getEventSender(e);
											if (target) {
												var focusTbl;
												target = target.parentNode;
												while (target) {
													if (target.tagName) {
														if (target.tagName.toLowerCase() == "table") {
															focusTbl = target;
															break;
														}
													}
													target = target.parentNode;
												}
												if (focusTbl) {
													if (focusTbl == tbl) {
														return;
													}
												}
											}
											tbl.style.display = 'none';
										}
										document.body.appendChild(tbl);
										_lookupTableElements[tname] = tbl;
										zi = JsonDataBinding.getPageZIndex(tbl) + 1;
										tbl.style.zIndex = zi;
										JsonDataBinding.AttachEvent(document, 'onmousedown', _onmousedown);
										var tbody;
										var tbds = tbl.getElementsByTagName('tbody');
										if (tbds) {
											if (tbds.length > 0) {
												tbody = tbds[0];
											}
										}
										if (!tbody) {
											tbody = document.createElement('tbody');
											tbl.appendChild(tbody);
										}
										var tr = tbody.insertRow(-1);
										var td = document.createElement("td");
										tr.appendChild(td);
										td.innerHTML = "<font color=red>Loading data from web server...</font>";
										tbl.cell = _buttonElement.cell;
										tbl.style.left = (_buttonElement.cell.cellLocation.x + document.body.scrollLeft) + 'px';
										tbl.style.top = (_buttonElement.offsetTop + _buttonElement.offsetHeight) + 'px';
										tbl.style.display = 'block';
										JsonDataBinding.executeServerMethod(tname, u);
									}
									else {
										tbl.cell = _buttonElement.cell;
										tbl.style.left = (_buttonElement.cell.cellLocation.x + document.body.scrollLeft) + 'px';
										tbl.style.top = (_buttonElement.offsetTop + _buttonElement.offsetHeight) + 'px';
										tbl.style.display = 'block';
									}
									JsonDataBinding.windowTools.updateDimensions();
									if (tbl.offsetHeight + tbl.offsetTop > JsonDataBinding.windowTools.pageDimensions.windowHeight()) {
										var newTop2 = _buttonElement.offsetTop - tbl.offsetHeight;
										if (newTop2 >= 0) {
											tbl.style.top = newTop2 + "px";
										}
									}
									tbl.focus();
								}
							}
						}
					}
					else if (_buttonElement.editor == EDITOR_CHKLIST) {
						if (!_chklstTableElements) {
							_chklstTableElements = {};
						}
						if (_buttonElement.cell) {
							if (_tblElement.FieldEditors) {
								var ed = _tblElement.FieldEditors[_buttonElement.cell.columnIndex];
								if (ed) {
									var tname2 = ed.TableName;
									tbl = _chklstTableElements[tname2];
									if (!tbl) {
										tbl = JsonDataBinding.createCheckedList(tname2, _tblElement, _buttonElement.cell.columnIndex);
										_chklstTableElements[tname2] = tbl;
										tbl.chklist.setPosition("absolute");
										ed.CellPainter = tbl.chklist;
										tbl.chklist.move((_buttonElement.cell.cellLocation.x + document.body.scrollLeft) + 'px', (_buttonElement.offsetTop + _buttonElement.offsetHeight) + 'px');
									}
									tbl.chklist.move((_buttonElement.cell.cellLocation.x + document.body.scrollLeft) + 'px', (_buttonElement.offsetTop + _buttonElement.offsetHeight) + 'px');
									tbl.chklist.loadData();
									tbl.chklist.move((_buttonElement.cell.cellLocation.x + document.body.scrollLeft) + 'px', (_buttonElement.offsetTop + _buttonElement.offsetHeight) + 'px');
									tbl.chklist.setDisplay("block");
									tbl.chklist.setzindex(parseInt(_buttonElement.style.zIndex ? _buttonElement.style.zIndex : 0) + 1);
									JsonDataBinding.windowTools.updateDimensions();
									if (tbl.chklist.getBottom() > JsonDataBinding.windowTools.pageDimensions.windowHeight()) {
										var newTop3 = _buttonElement.offsetTop - tbl.offsetHeight;
										if (newTop3 >= 0) {
											tbl.chklist.move((_buttonElement.cell.cellLocation.x + document.body.scrollLeft) + 'px', newTop3 + "px");
										}
									}
									zi = JsonDataBinding.getPageZIndex(tbl) + 1;
									tbl.style.zIndex = zi;
									tbl.focus();
								}
							}
						}
					}
				}
			}
		}
		//by mouse click or enter key, not by arrow keys
		function oncellselectionchange() {
			if (_selectionElement.selectedIndex >= 0 && _selectionElement.selectedIndex < _selectionElement.options.length) {
				var val = _selectionElement.options[_selectionElement.selectedIndex].value;
				var txt = JsonDataBinding.toText(_jsonTable.Rows[_selectionElement.cell.datarownum].ItemArray[_selectionElement.cell.columnIndex]);
				if (txt != val) {
					_backupdata(_jsonTable.TableName, _selectionElement.cell.datarownum);
					JsonDataBinding.SetInnerText(_selectionElement.cell, _selectionElement.options[_selectionElement.selectedIndex].text);
					_jsonTable.Rows[_selectionElement.cell.datarownum].changed = true;
					_jsonTable.Rows[_selectionElement.cell.datarownum].ItemArray[_selectionElement.cell.columnIndex] = val;
					if (_tblElement.ColumnValueChanged) {
						_tblElement.ColumnValueChanged(_tblElement, _selectionElement.cell.datarownum, _selectionElement.cell.columnIndex, val);
					}
				}
			}
			_selectionElement.style.display = 'none';
		}
		function oncellselectionclick() {
			oncellselectionchange();
		}
		function oncellselectionkeydown(e) {
			var evt = e || window.event;
			if (evt.keyCode == 13) {
				oncellselectionchange();
			}
		}
		function onCellClick(e) {
			if (typeof _textBoxElement != 'undefined') {
				_textBoxElement.cell = null;
				_textBoxElement.style.display = 'none';
			}
			if (typeof _buttonElement != 'undefined') {
				_buttonElement.cell = null;
				_buttonElement.style.display = 'none';
			}
			if (typeof _selectionElement != 'undefined') {
				_selectionElement.cell = null;
				_selectionElement.style.display = 'none';
			}
			if (typeof _datetimePickerButton != 'undefined') {
				_datetimePickerButton.cell = null;
				_datetimePickerButton.style.display = 'none';
			}
			if (!_lookupTableElements) {
				for (var nm in _lookupTableElements) {
					var t = _lookupTableElements[nm];
					if (t && t.tagName && t.tagName.toLowerCase() == 'table') {
						t.style.display = 'none';
					}
				}
			}
			var cell;
			if (e) {
				if (typeof e.datarownum != 'undefined') {
					cell = e;
				}
			}
			if (!cell) {
				cell = getCell(e);
			}
			if (cell) {
				var rowIndexChanged = (_jsonTable.rowIndex != cell.datarownum);
				_jsonTable.rowIndex = cell.datarownum;
				if (typeof cell.columnIndex != 'undefined' && !_tblElement.ReadOnly && typeof _textBoxElement != 'undefined' && typeof _buttonElement != 'undefined') {
					var cellReadOnly = false;
					if (typeof _jsonTable.Columns[cell.columnIndex].ReadOnly != 'undefined') {
						cellReadOnly = _jsonTable.Columns[cell.columnIndex].ReadOnly;
					}
					var editor = (cellReadOnly || _readOnly) ? EDITOR_NONE : EDITOR_TEXT;
					if (typeof _tblElement.FieldEditors != 'undefined' && _tblElement.FieldEditors != null) {
						if (typeof _tblElement.FieldEditors[cell.columnIndex] != 'undefined' && _tblElement.FieldEditors[cell.columnIndex] != null) {
							if (typeof _tblElement.FieldEditors[cell.columnIndex].Editor != 'undefined') {
								editor = _tblElement.FieldEditors[cell.columnIndex].Editor;
							}
						}
					}
					if (editor == EDITOR_NONE) {
						_textBoxElement.cell = null;
						_textBoxElement.style.display = 'none';
						_buttonElement.cell = null;
						_buttonElement.style.display = 'none';
					}
					else {
						_buttonElement.editor = editor;
						if (editor == EDITOR_TEXT || editor == EDITOR_DATETIME) {
							_textBoxElement.cell = null;
							_textBoxElement.disableMonitor = true;
							_textBoxElement.val = JsonDataBinding.GetInnerText(cell);
							_textBoxElement.value = JsonDataBinding.GetInnerText(cell);
							_textBoxElement.style.display = 'block';
							_textBoxElement.focus();
							cell.cellLocation = getCellLocation(cell);
							_textBoxElement.style.left = (cell.cellLocation.x + document.body.scrollLeft) + 'px';
							_textBoxElement.style.top = (cell.cellLocation.y + document.body.scrollTop) + 'px';
							_textBoxElement.style.width = cell.offsetWidth;
							_textBoxElement.cell = cell;
							//
							_textBoxElement.disableMonitor = false;
							//
							_buttonElement.cell = null;
							_buttonElement.style.display = 'none';
							//
							if (editor == EDITOR_DATETIME) {
								if (typeof _datetimePickerButton == 'undefined') {
									var dp = JsonDataBinding.GetDatetimePicker();
									if (typeof dp != 'undefined') {
										function onselecteddatetime(args) {
											if (args && args.date /*&& _textBoxElement.cell == cell*/) {
												_textBoxElement.value = JsonDataBinding.toText(args.date);
												ontextboxvaluechange();
											}
										}
										var fts = _tblElement.DatePickerFonstSize ? _tblElement.DatePickerFonstSize + 'px' : '12px';
										var opts = {
											formElements: {},
											showWeeks: true,
											statusFormat: "l-cc-sp-d-sp-F-sp-Y",
											bounds: { position: "absolute", inputRight: true, fontSize: fts, inputTime: true }
										};
										opts.formElements[_textBoxElement.id] = "Y-ds-m-ds-d";
										opts.callbackFunctions = { 'dateset': [onselecteddatetime] };
										_datetimePickerButton = dp.createDatePicker(opts);
									}
								}
								if (typeof _datetimePickerButton != 'undefined') {
									if (cell.offsetWidth > _datetimePickerButton.offsetWidth) {
										_textBoxElement.style.width = (cell.offsetWidth - _datetimePickerButton.offsetWidth) + 'px';
									}
									var cx = cell.cellLocation.x + document.body.scrollLeft;
									var cy = cell.cellLocation.y + document.body.scrollTop;
									if (cx + cell.offsetWidth > _datetimePickerButton.offsetWidth) {
										_datetimePickerButton.style.left = (cx + cell.offsetWidth - 22) + 'px';
									}
									else {
										_datetimePickerButton.style.left = cx + 'px';
									}
									_datetimePickerButton.style.top = cy + 'px';
									_datetimePickerButton.cell = cell;
									var zi = JsonDataBinding.getZOrder(_tblElement) + 1;
									_datetimePickerButton.style.zIndex = zi;
									_datetimePickerButton.style.display = 'block';
								}
							}
						}
						else if (editor == EDITOR_ENUM || editor == EDITOR_DBLOOKUP || editor == EDITOR_CHKLIST) {
							_textBoxElement.cell = null;
							_textBoxElement.style.display = 'none';
							//
							if (editor == EDITOR_ENUM) {
								_buttonElement.src = 'images/dropdownbutton.jpg';
							}
							else if (editor == EDITOR_DBLOOKUP) {
								_buttonElement.src = 'libjs/qry.jpg';
							}
							else if (editor == EDITOR_CHKLIST) {
								_buttonElement.src = 'libjs/chklist.jpg';
							}
							var cl = (cell.cellLocation.x + document.body.scrollLeft + cell.offsetWidth - _buttonElement.offsetWidth) + 'px';
							_buttonElement.style.left = cl;
							_buttonElement.style.top = (cell.cellLocation.y + document.body.scrollTop) + 'px';
							_buttonElement.style.width = '17px';
							_buttonElement.style.height = '15px';
							if (_buttonElement.offsetHeight > cell.offsetHeight) {
								_buttonElement.style.height = cell.offsetHeight;
								_buttonElement.style.width = _buttonElement.style.height;
							}
							_buttonElement.cell = cell;
							_buttonElement.style.display = 'block';
							if (_buttonElement.offsetHeight > cell.offsetHeight) {
								_buttonElement.style.height = cell.offsetHeight;
								_buttonElement.style.width = _buttonElement.style.height;
							}
							cl = (cell.cellLocation.x + document.body.scrollLeft + cell.offsetWidth - _buttonElement.offsetWidth) + 'px';
							_buttonElement.style.left = cl;
							var zi2 = JsonDataBinding.getZOrder(_tblElement) + 1;
							_buttonElement.style.zIndex = zi2;
						}
					}
				}
				if (_tblElement.TargetTable) {
					_tblElement.style.display = 'none';
					if (_tblElement.TargetTable.jsData && _tblElement.Editor && _tblElement.Editor.Map) {
						for (var i = 0; i < _tblElement.Editor.Map.length; i++) {
							var map = _tblElement.Editor.Map[i];
							var val = _getColumnValue(map[1]);
							_tblElement.TargetTable.jsData.setColumnValue(map[0], val);
						}
						if (_tblElement.TargetTable.DatabaseLookupSelected) {
							var cn = _tblElement.TargetTable.jsData.getColumnCount();
							var cid = -1;
							for (var i = 0; i < cn; i++) {
								if (_tblElement.Editor == _tblElement.TargetTable.FieldEditors[i]) {
									cid = i;
									break;
								}
							}
							_tblElement.TargetTable.DatabaseLookupSelected(_tblElement.TargetTable, cid);
						}
					}
				}
				else {
					if (rowIndexChanged) {
						JsonDataBinding.onRowIndexChange(_jsonTable.TableName);
					}
				}
			}
		}
		function _clickCell(cell) {
			var r = -1;
			if (cell) {
				r = cell.datarownum;
			}
			if (r >= 0 && r < _jsonTable.Rows.length) {
				if (r != _jsonTable.rowIndex) {
					_jsonTable.rowIndex = r;
					_onRowIndexChange(_jsonTable.TableName);
				}
				if (_selectedRow && _selectedRow.datarownum == r) {
					onCellClick(cell);
				}
			}
		}
		function _onRowIndexChange(name) {
			if (name == _jsonTable.TableName) {
				var tbody = JsonDataBinding.getTableBody(_tblElement);
				var bkc, cells, c;
				if (_selectedRow) {
					if (_selectedRow.datarownum != _jsonTable.rowIndex) {
						cells = _selectedRow.getElementsByTagName("TD");
						bkc = tbody.style.backgroundColor;
						if (_tblElement.AlternateBackgroundColor && _selectedRow.datarownum % 2 != 0) {
							bkc = _tblElement.AlternateBackgroundColor;
						}
						_selectedRow.style.backgroundColor = bkc;
						for (c = 0; c < cells.length; c++) {
							cells[c].style.backgroundColor = bkc;
						}
					}
				}
				_selectedRow = null;
				var r;
				var rn = tbody.rows.length;
				if (rn < _jsonTable.Rows.length) {
					for (r = rn; r < _jsonTable.Rows.length; r++) {
						addHtmlTableRow(tbody, r);
					}
				}
				for (r = 0; r < tbody.rows.length; r++) {
					if (typeof tbody.rows[r].datarownum != 'undefined') {
						if (tbody.rows[r].datarownum == _jsonTable.rowIndex) {
							_selectedRow = tbody.rows[r];
							if (typeof _textBoxElement != 'undefined') {
								if (typeof _textBoxElement.cell != 'undefined' && _textBoxElement.cell != null) {
									if (_textBoxElement.cell.datarownum != _jsonTable.rowIndex) {
										_textBoxElement.cell = null;
										_textBoxElement.style.display = 'none';
									}
								}
							}
							break;
						}
					}
				}
				if (_selectedRow) {
					if (_tblElement.SelectedRowColor) {
						bkc = _tblElement.SelectedRowColor;
					}
					else {
						bkc = tbody.style.backgroundColor;
					}
					_selectedRow.style.backgroundColor = bkc;
					var row = _jsonTable.Rows[_selectedRow.datarownum];
					var dataRowVer = typeof row.rowVersion == 'undefined' ? 0 : row.rowVersion;
					var viewRowVer = typeof _selectedRow.rowVersion == 'undefined' ? 0 : _selectedRow.rowVersion;
					cells = _selectedRow.getElementsByTagName("TD");
					if (viewRowVer < dataRowVer) {
						_selectedRow.rowVersion = dataRowVer;
						for (c = 0; c < cells.length && c < _jsonTable.Columns.length; c++) {
							cells[c].innerHTML = '';
							showCell(cells[c], c, row.ItemArray[c]);
						}
					}
					showActCtrls(cells);
					for (c = 0; c < cells.length; c++) {
						cells[c].style.backgroundColor = bkc;
					}
				}
			}
		}
		//other bound control modified value, update corresponding cell
		_tblElement.oncellvaluechange = function(name, r, c, value0) {
			if (_jsonTable.TableName == name) {
				var row;
				if (_selectedRow != null && _selectedRow.datarownum == r) {
					row = _selectedRow;
				}
				else {
					var tbody = JsonDataBinding.getTableBody(_tblElement);
					if (tbody) {
						if (r >= 0 && r < tbody.rows.length) {
							row = tbody.rows[r];
						}
					}
				}
				if (row) {
					var value = JsonDataBinding.toText(value0);
					//if (_selectedRow.datarownum == r) {
					if (_tblElement.FieldEditors) {
						if (_tblElement.FieldEditors[c]) {
							if (_tblElement.FieldEditors[c].CellPainter) {
								if (row.cells[c].showCell) {
									row.cells[c].showCell.updateCell(value);
									return;
								}
								else {
									row.cells[c].showCell = _tblElement.FieldEditors[c].CellPainter.showCell(row.cells[c], c, value);
									return;
								}
							}
						}
					}
					if (typeof row.cells[c].childNodes != 'undefined') {
						for (var i = 0; i < row.cells[c].childNodes.length; i++) {
							if (row.cells[c].childNodes[i].nodeName == '#text') {
								row.cells[c].childNodes[i].nodeValue = value;
								if (_textBoxElement && _textBoxElement.cell == row.cells[c]) {
									if (_textBoxElement.value != value) {
										_textBoxElement.value = value;
									}
								}
								break;
							}
						}
					}
					//}
				}
			}
		}
		function onrowdelete(name, r0) {
			if (_jsonTable.TableName == name) {
				for (var r = 0; r < _tblElement.rows.length; r++) {
					if (typeof _tblElement.rows[r].datarownum != 'undefined') {
						if (_tblElement.rows[r].datarownum == r0) {
							_tblElement.rows[r].style.display = 'none';
							if (_textBoxElement.cell != 'undefined' && _textBoxElement.cell != null) {
								if (_textBoxElement.cell.datarownum == r0) {
									_textBoxElement.cell = null;
									_textBoxElement.style.display = 'none';
								}
							}
							break;
						}
					}
				}
			}
		}
		function _onSelectedRowColorChanged() {
			if (_selectedRow != null) {
				var tbody = JsonDataBinding.getTableBody(_tblElement);
				if (tbody) {
					var bkc;
					if (_tblElement.SelectedRowColor) {
						bkc = _tblElement.SelectedRowColor;
					}
					else {
						if (_tblElement.AlternateBackgroundColor && _selectedRow.datarownum % 2 != 0) {
							bkc = _tblElement.AlternateBackgroundColor;
						}
						else {
							bkc = tbody.style.backgroundColor;
						}
					}
					_selectedRow.style.backgroundColor = bkc;
					for (var j = 0; j < _selectedRow.cells.length; j++) {
						_selectedRow.cells[j].style.backgroundColor = bkc;
					}
				}
			}
		}
		function _onRowColorChanged() {
			var tbody = JsonDataBinding.getTableBody(_tblElement);
			if (tbody) {
				if (tbody.rows) {
					for (var i = 0; i < tbody.rows.length; i++) {
						if (_selectedRow && _selectedRow.datarownum == i) {
							continue;
						}
						var bkc;
						if (_tblElement.AlternateBackgroundColor && tbody.rows[i].datarownum % 2 != 0) {
							bkc = _tblElement.AlternateBackgroundColor;
						}
						else {
							bkc = tbody.style.backgroundColor;
						}
						tbody.rows[i].style.backgroundColor = bkc;
						for (var j = 0; j < tbody.rows[i].cells.length; j++) {
							tbody.rows[i].cells[j].style.backgroundColor = bkc;
						}
					}
					_onSelectedRowColorChanged();
				}
			}
		}
		function _setEventHandler(eventName, func) {
			if (eventName == 'ColumnValueChanged') {
				eventCellValueChanged = func;
			}
		}
		function _getColumnValueByColumnIndex(columnIndex) {
			if (_jsonTable.rowIndex >= 0 && _jsonTable.rowIndex < _jsonTable.Rows.length) {
				//return _jsonTable.Rows[_jsonTable.rowIndex].ItemArray[columnIndex];
				return JsonDataBinding.columnValueByIndex(_jsonTable.TableName, columnIndex, _jsonTable.rowIndex);
			}
			return null;
		}
		function _getColumnValue(columnName) {
			return _getColumnValueByColumnIndex(_columnNameToIndex(_jsonTable.TableName, columnName));
			//if (_jsonTable.rowIndex >= 0 && _jsonTable.rowIndex < _jsonTable.Rows.length) {
			//	return _jsonTable.Rows[_jsonTable.rowIndex].ItemArray[_columnNameToIndex(_jsonTable.TableName, columnName)];
			//}
			//return null;
		}
		function _setColumnValueByColumnIndex(columnIndex, val) {
			if (_jsonTable.rowIndex >= 0 && _jsonTable.rowIndex < _jsonTable.Rows.length) {
				_backupdata(_jsonTable.TableName, _jsonTable.rowIndex);
				_jsonTable.Rows[_jsonTable.rowIndex].ItemArray[columnIndex] = val;
				_jsonTable.Rows[_jsonTable.rowIndex].changed = true;
				JsonDataBinding.onvaluechanged(_jsonTable, _jsonTable.rowIndex, columnIndex, val);
			}
		}
		function _setColumnValue(columnName, val) {
			_setColumnValueByColumnIndex(_columnNameToIndex(_jsonTable.TableName, columnName), val);
		}
		function _getTableName() {
			return _jsonTable.TableName;
		}
		function _getModifiedRowCount() {
			var r0 = 0;
			for (var r = 0; r < _jsonTable.Rows.length; r++) {
				if (_jsonTable.Rows[r].changed) {
					r0++;
				}
			}
			return r0;
		}
		function _getDeletedRowCount() {
			var r0 = 0;
			for (var r = 0; r < _jsonTable.Rows.length; r++) {
				if (_jsonTable.Rows[r].deleted) {
					r0++;
				}
			}
			return r0;
		}
		function _getNewRowCount() {
			var r0 = 0;
			for (var r = 0; r < _jsonTable.Rows.length; r++) {
				if (_jsonTable.Rows[r].added) {
					r0++;
				}
			}
			return r0;
		}
		function _getRowCount() {
			var r0 = _jsonTable.Rows.length;
			for (var r = 0; r < _jsonTable.Rows.length; r++) {
				if (_jsonTable.Rows[r].deleted) {
					r0--;
				}
			}
			return r0;
		}
		//---text editor---
		_textBoxElement = document.createElement('input');
		_textBoxElement.id = 'txt' + _tblElement.id;
		_textBoxElement.type = 'text';
		_textBoxElement.style.position = 'absolute';
		var zi = JsonDataBinding.getZOrder(_tblElement) + 1;
		_textBoxElement.style.zIndex = zi;
		JsonDataBinding.AttachEvent(_textBoxElement, 'onchange', ontextboxvaluechange);
		document.body.appendChild(_textBoxElement);

		//tbd.appendChild(_textBoxElement);
		_textBoxElement.style.display = 'none';
		JsonDataBinding.addTextBoxObserver(_textBoxElement);
		//---endof text editor---
		//---button---
		_buttonElement = document.createElement('img');
		// _buttonElement.type = 'button';
		_buttonElement.style.position = 'absolute';
		_buttonElement.style.cursor = 'pointer';
		//_buttonElement.value = '';
		zi = JsonDataBinding.getZOrder(_tblElement) + 1;
		_buttonElement.style.zIndex = zi;
		//var btimg = document.createElement('img');
		_buttonElement.src = 'images/dropdownbutton.jpg';
		//_buttonElement.appendChild(btimg);
		document.body.appendChild(_buttonElement);
		//tbd.appendChild(_buttonElement);
		_buttonElement.style.display = 'none';
		JsonDataBinding.AttachEvent(_buttonElement, 'onclick', oncellbuttonclick);
		//---end of button---
		function _onmousedownForEnum(e) {
			if (!_selectionElement) return;
			var target = getEventSender(e);
			if (target) {
				while (target) {
					if (_selectionElement == target) return;
					target = target.parentNode;
				}
			}
			_selectionElement.style.display = 'none';
		}
		function _setColumnVisible(cidx, visible) {
			var changed = false;
			if (visible) {
				if (_tblElement.InvisibleColumns && _tblElement.InvisibleColumns[cidx]) {
					changed = true;
				}
			}
			else {
				if (!_tblElement.InvisibleColumns) {
					_tblElement.InvisibleColumns = {};
					_tblElement.InvisibleColumns[cidx] = true;
					changed = true;
				}
				else if (!_tblElement.InvisibleColumns[cidx]) {
					_tblElement.InvisibleColumns[cidx] = true;
					changed = true;
				}
			}
			if (changed) {
				var i;
				var stl = visible ? 'block' : 'none';
				var th = _tblElement.tHead;
				if (th && th.rows) {
					for (i = 0; i < th.rows.length; i++) {
						if (th.rows[i].cells && th.rows[i].cells.length > cidx) {
							th.rows[i].cells[cidx].style.display = stl;
						}
					}
				}
				var tbody = JsonDataBinding.getTableBody(_tblElement);
				if (tbody && tbody.rows) {
					for (i = 0; i < tbody.rows.length; i++) {
						if (tbody.rows[i].cells && tbody.rows[i].cells.length > cidx) {
							tbody.rows[i].cells[cidx].style.display = stl;
						}
					}
				}
			}
		}
		function _refreshBindColumnDisplay(name, rowidx, colIdx) {
			if (_jsonTable.TableName == name) {
				var tbody = JsonDataBinding.getTableBody(_tblElement);
				if (tbody && tbody.rows) {
					for (var r = 0; r < tbody.rows.length; r++) {
						if (tbody.rows[r].datarownum == rowidx) {
							var allcells = typeof colIdx == 'undefined';
							if (allcells)
								colIdx = 0;
							while (tbody.rows[r].cells && tbody.rows[r].cells.length > colIdx) {
								if (tbody.rows[r].cells[colIdx].showCell) {
									tbody.rows[r].cells[colIdx].showCell.updateCell(JsonDataBinding.toText(_jsonTable.Rows[rowidx].ItemArray[colIdx]));
								}
								else {
									var vset = false;
									var c = colIdx;
									var value = JsonDataBinding.toText(_jsonTable.Rows[rowidx].ItemArray[colIdx]);
									if (typeof tbody.rows[r].cells[c].childNodes != 'undefined') {
										for (var i = 0; i < tbody.rows[r].cells[c].childNodes.length; i++) {
											if (tbody.rows[r].cells[c].childNodes[i].nodeName == '#text') {
												tbody.rows[r].cells[c].childNodes[i].nodeValue = value;
												if (_textBoxElement && _textBoxElement.cell == tbody.rows[r].cells[c]) {
													if (_textBoxElement.value != value) {
														_textBoxElement.value = value;
													}
												}
												if (allcells)
													vset = true;
												else
													return;
											}
										}
									}
									if (!vset)
										tbody.rows[r].cells[c].innerHTML = value;
								}
								if (allcells)
									colIdx++;
								else
									break;
							}
							break;
						}
					}
				}
			}
		}
		function _setReadOnly(rdonly) {
			if (_tblElement) {
				_tblElement.ReadOnly = rdonly;
				if (rdonly) {
					if (_textBoxElement) {
						_textBoxElement.cell = null;
						_textBoxElement.style.display = 'none';
					}
				}
			}
		}
		JsonDataBinding.AttachEvent(document, 'onmousedown', _onmousedownForEnum);
		//
		init();
		return {
			getColumnCount: function() {
				if (_jsonTable)
					return _jsonTable.Columns.length;
			},
			getSelectedRowElement: function() {
				return _selectedRow;
			},
			onRowIndexChange: function(name) {
				_onRowIndexChange(name);
			},
			oncellvaluechange: function(name, r, c, value) {
				//                var tn = _getTableName();
				//                var tn2 = this.getTableName();
				//                if (tn != tn2) {
				//                }
				_oncellvaluechange(name, r, c, value);
			},
			onDataReady: function(jsData) {
				_onDataReady(jsData);
			},
			onRowColorChanged: function() {
				_onRowColorChanged();
			},
			onSelectedRowColorChanged: function() {
				_onSelectedRowColorChanged();
			},
			setEventHandler: function(eventName, func) {
				_setEventHandler(eventName, func);
			},
			getColumnValue: function(columnName) {
				return _getColumnValue(columnName);
			},
			getColumnValueByColumnIndex: function(columnIndex) {
				return _getColumnValueByColumnIndex(columnIndex);
			},
			setColumnValue: function(columnName, val) {
				_setColumnValue(columnName, val);
			},
			setColumnValueByColumnIndex: function(columnIndex, val) {
				_setColumnValueByColumnIndex(columnIndex, val);
			},
			getTableName: function() {
				return _getTableName();
			},
			clickCell: function(cell) {
				_clickCell(cell);
			},
			getModifiedRowCount: function() {
				return _getModifiedRowCount();
			},
			getDeletedRowCount: function() {
				return _getDeletedRowCount();
			},
			getNewRowCount: function() {
				return _getNewRowCount();
			},
			setColumnVisible: function(cidx, visible) {
				_setColumnVisible(cidx, visible);
			},
			refreshBindColumnDisplay: function(name, rowidx, colIdx) {
				_refreshBindColumnDisplay(name, rowidx, colIdx);
			},
			setReadOnly: function (rdonly) {
				_setReadOnly(rdonly);
			},
			adjustHeaderWidthes: function () {
				_adjustColumnWidthes();
			}
		}
	},
	//end of table binding ===============
	//html listbox (select) for SelectedItem and SelectedValue======================================
	//listElement: html element
	//jsTable: json data table
	HtmlListboxData: function(listElement, jsTable, itemFieldIdx, valueFieldIdx) {
		var _listElement = listElement;
		var _jsonTable = jsTable
		var _itemFieldIdx = itemFieldIdx;
		var _valueFieldIdx = valueFieldIdx;
		var _useBlank = listElement.getAttribute('useblank');
		function init() {
			JsonDataBinding.addvaluechangehandler(_jsonTable.TableName, _listElement);
			JsonDataBinding.AttachOnRowDeleteHandler(_jsonTable.TableName, onrowdelete);
			recreateListboxElements();
			if (_listElement.parentNode && _listElement.parentNode.jsData && _listElement.parentNode.jsData.onChildListBoxFilled) {
				_listElement.parentNode.jsData.onChildListBoxFilled();
			}
		}
		function onrowdelete(name, r0) {
			if (_jsonTable.TableName == name) {
				var items = _listElement.getElementsByTagName('option');
				for (var r = 0; r < items.length; r++) {
					if (typeof items[r].datarownum != 'undefined') {
						if (items[r].datarownum == r0) {
							_listElement.remove(r);
							break;
						}
					}
				}
			}
		}
		function getItem(e) {
			return getEventSender(e);
		}
		function onItemMouseOver(e) {
			var op = getItem(e);
			if (typeof op != 'undefined') {
				if (_listElement.HighlightRowColor) {
					op.style.backgroundColor = _listElement.HighlightRowColor;
				}
			}
		}
		function onItemMouseOut(e) {
			var op = getItem(e);
			if (typeof op != 'undefined') {
				var bkC;
				if (typeof _jsonTable.rowIndex != 'undefined' && op.datarownum == _jsonTable.rowIndex) {
					if (_listElement.SelectedRowColor) {
						bkC = _listElement.SelectedRowColor;
					}
					else {
						bkC = _listElement.style.backgroundColor;
					}
				}
				else {
					bkC = _listElement.style.backgroundColor;
				}
				op.style.backgroundColor = bkC;
			}
		}
		var addingMethod;
		function addElement(r) {
			var isDeleted = (_jsonTable.Rows[r].deleted || _jsonTable.Rows[r].removed);
			if (!isDeleted) {
				var text;
				var val;
				if (typeof _jsonTable.Rows[r].ItemArray[_itemFieldIdx] != 'undefined' && _jsonTable.Rows[r].ItemArray[_itemFieldIdx] != null) {
					text = JsonDataBinding.toText(_jsonTable.Rows[r].ItemArray[_itemFieldIdx]);
				}
				else {
					text = '';
				}
				if (typeof _jsonTable.Rows[r].ItemArray[_valueFieldIdx] != 'undefined' && _jsonTable.Rows[r].ItemArray[_valueFieldIdx] != null) {
					val = _jsonTable.Rows[r].ItemArray[_valueFieldIdx];
				}
				else {
					val = '';
				}
				var op = document.createElement('option');
				op.text = text;
				op.value = val;
				op.datarownum = r;
				if (!_useBlank && _jsonTable.rowIndex == r) {
					op.selected = true;
				}
				if (typeof addingMethod == 'undefined') {
					try {
						_listElement.add(op, null);
						addingMethod = true;
					}
					catch (e) {
						_listElement.add(op);
						addingMethod = false;
					}
				}
				else {
					if (addingMethod) {
						_listElement.add(op, null);
					}
					else {
						_listElement.add(op);
					}
				}
			}
		}
		function recreateListboxElements() {
			_listElement.options.length = 0;
			_useBlank = _listElement.getAttribute('useblank');
			if (_useBlank) {
				var op = document.createElement('option');
				op.text = '';
				op.value = null;
				op.datarownum = -1;
				op.selected = true;
				if (typeof addingMethod == 'undefined') {
					try {
						_listElement.add(op, null);
						addingMethod = true;
					}
					catch (e) {
						_listElement.add(op);
						addingMethod = false;
					}
				}
				else {
					if (addingMethod) {
						_listElement.add(op, null);
					}
					else {
						_listElement.add(op);
					}
				}
			}
			for (var r = 0; r < _jsonTable.Rows.length; r++) {
				addElement(r);
			}
		}
		function onSelectedIndexChanged() {
			if (_listElement.selectedIndex >= 0) {
				var op = _listElement.options[_listElement.selectedIndex];
				if (typeof op != 'undefined' && op.datarownum != 'undefined') {
					_jsonTable.rowIndex = op.datarownum;
					JsonDataBinding.onRowIndexChange(_jsonTable.TableName);
				}
			}
		}
		function _onRowIndexChange(name) {
			if (name == _jsonTable.TableName) {
				//add new rows
				var r;
				var rn = _listElement.options.length;
				var lastItem = _listElement.options[rn - 1];
				var rn0 = lastItem.datarownum;
				if (rn0 < _jsonTable.Rows.length) {
					for (r = rn0 + 1; r < _jsonTable.Rows.length; r++) {
						addElement(r);
					}
				}
				//select the option to current row index
				var r0 = _jsonTable.rowIndex;
				var items = _listElement.getElementsByTagName('option');
				//var isNew = true;
				for (var r = 0; r < items.length; r++) {
					if (typeof items[r].datarownum != 'undefined') {
						if (items[r].datarownum == r0) {
							items[r].selected = true;
							//			isNew = false;
							break;
						}
					}
				}
				//if (isNew) {
				//	addElement(r0);
				//}
			}
		}
		function _onDataReady(jsData) {
			_jsonTable = jsData;
			init();
		}
		_listElement.oncellvaluechange = function(name, r, c, value) {
			if ((c == _itemFieldIdx || c == _valueFieldIdx) && _jsonTable.TableName == name) {
				var items = _listElement.getElementsByTagName('option');
				for (var r0 = 0; r0 < items.length; r0++) {
					if (typeof items[r0].datarownum != 'undefined') {
						if (items[r0].datarownum == r) {
							if (c == _itemFieldIdx) {
								items[r0].text = value;
							}
							else if (c == _valueFieldIdx) {
								items[r0].value = value;
							}
							break;
						}
					}
				}
			}
		}
		init();
		JsonDataBinding.AttachEvent(_listElement, 'onchange', onSelectedIndexChanged);
		return {
			onRowIndexChange: function(name) {
				_onRowIndexChange(name);
			},
			onDataReady: function(jsData) {
				_onDataReady(jsData);
			}
		}
	},
	//end of listbox binding ===============
	FileSizeInLimit: function(maxBytes, fileControl) {
		if (fileControl.files && fileControl.files.length == 1) {
			if (fileControl.files[0].size > maxBytes) {
				//alert("The file must be less than " + (maxBytes / 1024 / 1024) + "MB");
				return false;
			}
		}
		//alert("size ok");
		return true;
	},
	CreateFileUploader: function(fileInput) {
		function getFileSizeControl() {
			var form = fileInput.parentNode;
			if (form) {
				for (var i = 0; i < form.children.length; i++) {
					if (form.children[i].name == 'MAX_FILE_SIZE') {
						return form.children[i];
					}
				}
			}
		}
		return {
			IsFileSizeValid: function(showError) {
				var sc = getFileSizeControl();
				if (sc) {
					var maxSize = parseInt(sc.getAttribute("value"));
					if (maxSize <= 0) {
						maxSize = 1024;
					}
					if (JsonDataBinding.FileSizeInLimit(maxSize, fileInput)) {
						return true;
					}
					else {
						if (showError) {
							if (maxSize >= 1048576) {
								alert("The file size must be less than " + (maxSize / 1024 / 1024) + " MB");
							}
							else {
								alert("The file size must be less than " + (maxSize / 1024) + " KB");
							}
						}
					}
				}
				return false;
			},
			SetMaxFileSize: function(msize) {
				var sc = getFileSizeControl();
				if (sc) {
					sc.setAttribute('value', msize);
				}
			},
			GetMaxFileSize: function() {
				var sc = getFileSizeControl();
				if (sc) {
					return sc.getAttribute('value');
				}
			}
		};
	},
	//files uploader =================
	FilesUploader: function(formName, imgWidth, displayElementName, displayImagePath) {
		var _formName = formName;
		var _width = imgWidth;
		var fform = document.getElementById(_formName);
		var _displayElementName = displayElementName;
		var _displayImagePath = displayImagePath;
		function createFileInput() {
			//            var fform = document.getElementById(_formName);
			var f = document.createElement("input");
			f.type = 'file';
			f.style.cssText = "text-align: right; cursor:pointer; opacity:0.0;filter:alpha(opacity=0);moz-opacity:0; z-index: 1; position: absolute; left:0px; top:0px; width:" + _width + "px;";
			f.style.zIndex = 1;
			f.onchange = function(event) { onFileSelected(event); };
			f.name = _formName + "[]";
			fform.appendChild(f);
			if (IsFireFox()) {
				f.style.left = '-140px';
			}
		}
		function addFileToDisplay(fn) {
			if (_displayElementName) {
				var sp1 = document.getElementById(_displayElementName);
				var sp = document.createElement("span");
				if (_displayImagePath) {
					var img = document.createElement("img");
					img.src = _displayImagePath;
					sp.appendChild(img);
				}
				var spt = document.createElement("span");
				spt.innerHTML = "<b>" + fn + ",</b>";
				sp.appendChild(spt);
				sp1.appendChild(sp);
			}
		}
		function clearDisplay() {
			if (_displayElementName) {
				var sp1 = document.getElementById(_displayElementName);
				sp1.innerHTML = '';
			}
		}
		function onFileSelected(e) {
			//            var c;
			//            if (!e) e = window.event;
			//            if (e.target) c = e.target;
			//            else if (e.srcElement) c = e.srcElement;
			//            if (typeof c != 'undefined') {
			//                if (c.nodeType == 3)
			//                    c = c.parentNode;
			//            }
			var c = getEventSender(e);
			var fn = c.value;
			//            var fform = document.getElementById(_formName);
			var inputs = fform.getElementsByTagName("input");
			var found = false;
			for (var i = 0; i < inputs.length; i++) {
				if (inputs[i].type.toLowerCase() == "file") {
					// alert('file ' + i + ':' + inputs[i].value + ', z:' + inputs[i].style.zIndex);
					if (inputs[i].style.zIndex == 0) {
						if (inputs[i].value == fn) {
							found = true;
							break;
						}
					}
				}
			}
			if (found) {
				fform.removeChild(c);
			}
			else {
				c.style.zIndex = 0;
				//
				addFileToDisplay(fn);
				if (fform.onFileSelected) {
					fform.onFileSelected();
				}
			}
			createFileInput();
		}
		function init() {
			createFileInput();
		}
		function _clearFiles() {
			//            var fform = document.getElementById(_formName);

			var found = false;
			for (var k = 0; k < 3; k++) {
				var inputs = fform.getElementsByTagName("input");
				for (var i = 0; i < inputs.length; i++) {
					if (inputs[i].type.toLowerCase() == "file") {
						fform.removeChild(inputs[i]);
					}
				}
			}
			clearDisplay();
			//            alert(fform.innerHTML);
			createFileInput();
		}
		function _removeFile(filePath) {
			if (filePath && filePath.toLowerCase) {
				filePath = filePath.replace(',', '').toLowerCase();

				var f2 = filePath.replace(/^.*\\/, '');
				//                var fform = document.getElementById(_formName);
				var inputs = fform.getElementsByTagName("input");
				var found = false;
				var i;
				for (i = 0; i < inputs.length; i++) {
					if (inputs[i].type.toLowerCase() == "file") {
						if (inputs[i].style.zIndex == 0) {
							var isFile = false;
							if (IsFireFox() || IsChrome()) {
								var filename = inputs[i].value.replace(/^.*\\/, '').toLowerCase();
								isFile = (filename == f2);
							}
							else {
								isFile = (filePath == inputs[i].value.toLowerCase());
							}
							if (isFile) {
								fform.removeChild(inputs[i]);
								found = true;
							}
						}
					}
				}
				if (found) {
					if (_displayElementName) {
						var sp1 = document.getElementById(_displayElementName);
						sp1.innerHTML = '';
						inputs = fform.getElementsByTagName("input");
						for (i = 0; i < inputs.length; i++) {
							if (inputs[i].value && inputs[i].value.length > 0) {
								addFileToDisplay(inputs[i].value);
							}
						}
					}
				}
			}
		}
		function getfiles() {
			var files = new Array();
			//            var fform = document.getElementById(_formName);
			var inputs = fform.getElementsByTagName("input");
			for (var i = 0; i < inputs.length; i++) {
				if (inputs[i].value && inputs[i].value.length > 0) {
					files.push(inputs[i].value);
				}
			}
			return files;
		}
		init();
		return {
			ClearFiles: function() {
				_clearFiles();
			},
			RemoveFile: function(filePath) {
				_removeFile(filePath);
			},
			SelectedFiles: function() {
				return getfiles();
			}
		}
	},
	//end of FilesUploader
	//===checked list box ===
	createCheckedList: function(name, targetTable, idx) {
		var _tagetTable = targetTable;
		var _columnIndex = idx;
		var _editor = _tagetTable.FieldEditors[idx];
		var div = document.createElement("div");
		document.body.appendChild(div);
		div.style.border = "1px solid black";
		div.style.backgroundColor = "white";
		div.style.display = 'none';
		var imgok = document.createElement("img");
		div.appendChild(imgok);
		imgok.src = "libjs/ok.png";
		imgok.style.cursor = "pointer";
		var imgCancel = document.createElement("img");
		div.appendChild(imgCancel);
		imgCancel.src = "libjs/cancel.png";
		imgCancel.style.cursor = "pointer";
		var spanMsg = document.createElement('span');
		div.appendChild(spanMsg);
		spanMsg.innerHTML = "<font color=red>Loading data...</font>";
		var tbl = document.createElement("table");
		div.appendChild(tbl);
		tbl.id = name;
		tbl.border = 0;
		tbl.frame = "void";
		tbl.style.display = "block";
		tbl.setAttribute('readonly', 'true');
		tbl.setAttribute('jsdb', name);
		function _onmousedown(e) {
			var target = getEventSender(e);
			if (target) {
				var focusTbl;
				target = target.parentNode;
				while (target) {
					if (target.tagName) {
						if (target.tagName.toLowerCase() == "div") {
							focusTbl = target;
							break;
						}
					}
					target = target.parentNode;
				}
				if (focusTbl) {
					if (focusTbl == div) {
						return;
					}
				}
			}
			div.style.display = 'none';
		}
		var tbody;
		var tbd = tbl.getElementsByTagName('tbody');
		if (tbd) {
			if (tbd.length > 0) {
				tbody = tbd[0];
			}
		}
		if (!tbody) {
			tbody = document.createElement('tbody');
			tbl.appendChild(tbody);
		}
		function getCheckedItems() {
			var s;
			for (var r = 0; r < tbody.rows.length; r++) {
				var chks = tbody.rows[r].getElementsByTagName("input");
				if (chks[0].checked) {
					var sps = tbody.rows[r].getElementsByTagName("span");
					if (s) {
						s += ",";
						s += sps[0].innerHTML;
					}
					else {
						s = sps[0].innerHTML;
					}
				}
			}
			if (!s) {
				s = '';
			}
			return s;
		}
		function onOK() {
			var s = getCheckedItems();
			_tagetTable.jsData.setColumnValueByColumnIndex(_columnIndex, s);
			div.style.display = 'none';
		}
		function _setCheckedByTexts(texts) {
			var r, chks;
			for (r = 0; r < tbody.rows.length; r++) {
				chks = tbody.rows[r].getElementsByTagName("input");
				chks[0].checked = false;
			}
			if (texts && texts != '') {
				var ts = texts.split(',');
				for (var i = 0; i < ts.length; i++) {
					for (r = 0; r < tbody.rows.length; r++) {
						var sps = tbody.rows[r].getElementsByTagName("span");
						if (sps[0].innerHTML == ts[i]) {
							chks = tbody.rows[r].getElementsByTagName("input");
							chks[0].checked = true;
							break;
						}
					}
				}
			}
		}
		function _applyTargetdata() {
			if (_tagetTable.jsData) {
				var s = _tagetTable.jsData.getColumnValueByColumnIndex(_columnIndex);
				_setCheckedByTexts(s);
			}
		}
		function _setMessage(msg) {
			spanMsg.innerHTML = msg;
		}
		function _addrow(text, value, checked) {
			var tr = tbody.insertRow(-1);
			var td = document.createElement('td');
			tr.appendChild(td);
			var chk = document.createElement('input');
			chk.type = "checkbox";
			td.appendChild(chk);
			var span = document.createElement('span');
			span.innerHTML = text;
			td.appendChild(span);
			var val = document.createElement('input');
			val.type = "hidden";
			val.value = value;
			td.appendChild(val);
			if (checked) {
				chk.checked = true;
			}
		}
		JsonDataBinding.AttachEvent(document, 'onmousedown', _onmousedown);
		JsonDataBinding.AttachEvent(imgCancel, 'onclick', function() { div.style.display = 'none'; });
		JsonDataBinding.AttachEvent(imgok, 'onclick', onOK);
		var _dataLoaded = false;
		tbl.chklist = {
			dataLoaded: function() {
				return _dataLoaded;
			},
			getTable: function() {
				return tbl;
			},
			setMessage: function(msg) {
				_setMessage(msg);
			},
			setPosition: function(pos) {
				div.style.position = pos;
			},
			move: function(x, y) {
				div.style.left = x;
				div.style.top = y;
			},
			getBottom: function() {
				return div.offsetHeight + div.offsetTop;
			},
			setDisplay: function(dis) {
				div.style.display = dis;
			},
			setzindex: function(zi) {
				div.style.zIndex = zi;
			},
			addrow: function(text, value, checked) {
				_addrow(text, value, checked);
			},
			loadData: function() {
				if (_dataLoaded) {
					_applyTargetdata();
				}
				else {
					_dataLoaded = true;
					if (_editor.UseDb) {
						var u = new Object();
						JsonDataBinding.executeServerMethod(_editor.TableName, u);
					}
					else {
						for (var i = 0; i < _editor.List.length; i++) {
							_addrow(_editor.List[i], i);
						}
						_setMessage("");
						_applyTargetdata();
					}
				}
			},
			loadRecords: function(records) {
				for (var i = 0; i < records.length; i++) {
					_addrow(records[i].ItemArray[0], i);
				}
				_setMessage("");
				_applyTargetdata();
			},
			setCheckedByTexts: function(texts) {
				_setCheckedByTexts(texts);
			},
			applyTargetdata: function() {
				_applyTargetdata();
			},
			showCell: function(td, c, val) {
				var _cell = td;
				var imgplus;
				var txt0;
				var divContents;
				function plusclick() {
					if (divContents.style.display == 'block') {
						divContents.style.display = 'none';
						imgplus.src = 'libjs/plus.gif';
					}
					else {
						divContents.style.display = 'block';
						imgplus.src = 'libjs/minus.gif';
					}
				}
				if (typeof val == 'undefined' || val == null) {
					td.appendChild(document.createTextNode(JsonDataBinding.NullDisplayText));
				}
				else {
					var s = '' + JsonDataBinding.toText(val);
					if (s.length < 20) {
						var txt = document.createTextNode(s);
						td.appendChild(txt);
					}
					else {
						var s0 = s.substr(0, 6) + ' ...';
						var ss = s.replace(new RegExp(',', 'g'), '<br>');
						imgplus = document.createElement("img");
						imgplus.src = 'libjs/plus.gif';
						td.appendChild(imgplus);
						imgplus.style.display = 'block';
						imgplus.style.cursor = 'pointer';
						imgplus.align = 'left';
						txt0 = document.createTextNode(s0);
						td.appendChild(txt0);
						divContents = document.createElement("div");
						td.appendChild(divContents);
						divContents.style.display = 'none';
						divContents.innerHTML = ss;
						divContents.ownerCell = _cell;
						var clickCell = function() {
							_tagetTable.jsData.clickCell(_cell);
						};
						JsonDataBinding.AttachEvent(imgplus, 'onclick', plusclick);
						//JsonDataBinding.AttachEvent(divContents, 'onclick', clickCell);
					}
				}
				return {
					updateCell: function(val) {
						for (var i = 0; i < _cell.childNodes.length; i++) {
							if (_cell.childNodes[i].nodeValue) {
								_cell.childNodes[i].nodeValue = '';
							}
						}
						if (typeof val == 'undefined' || val == null) {
							if (imgplus) {
								imgplus.style.display = 'none';
							}
							if (divContents) {
								divContents.style.display = 'none';
							}
							if (!txt0) {
								txt0 = document.createTextNode(JsonDataBinding.NullDisplayText);
								_cell.appendChild(txt0);
							}
							else {
								txt0.nodeValue = JsonDataBinding.NullDisplayText;
							}
						}
						else {
							var s = '' + JsonDataBinding.toText(val);
							if (s.length < 20) {
								if (imgplus) {
									imgplus.style.display = 'none';
								}
								if (divContents) {
									divContents.style.display = 'none';
								}
								if (!txt0) {
									txt0 = document.createTextNode(s);
									td.appendChild(txt0);
								}
								else {
									txt0.nodeValue = s;
								}
							}
							else {
								var s0 = s.substr(0, 6) + ' ...';
								var ss = s.replace(new RegExp(',', 'g'), '<br>');
								var expended = false;
								if (divContents) {
									if (divContents.style.display == 'block') {
										expended = true;
									}
								}
								if (!imgplus) {
									imgplus = document.createElement("img");
									_cell.appendChild(imgplus);
									imgplus.style.cursor = 'pointer';
									imgplus.align = 'left';
									JsonDataBinding.AttachEvent(imgplus, 'onclick', plusclick);
								}
								if (expended) {
									imgplus.src = 'libjs/minus.gif';
								}
								else {
									imgplus.src = 'libjs/plus.gif';
								}
								imgplus.style.display = 'block';
								if (!txt0) {
									txt0 = document.createTextNode(s0);
									_cell.appendChild(txt0);
								}
								else {
									txt0.nodeValue = s0;
								}
								if (!divContents) {
									divContents = document.createElement("div");
									_cell.appendChild(divContents);
									divContents.ownerCell = _cell;
								}
								divContents.innerHTML = ss;
								if (expended) {
									divContents.style.display = 'block';
								}
								else {
									divContents.style.display = 'none';
								}
							}
						}
					}
				};
			}
		}
		return tbl;
	},
	//===end of checked listbox ===
	//div as data repeater ======================================
	//divElement: html div element
	//jsTable: json data table
	DataRepeater: function(divElement, jsTable) {
		var _divElement = divElement;
		var _jsonTable = jsTable;
		var _readOnly = false;
		var _pageIndex = 0;
		var _itemCount = 0;
		var _currentGroupIndex = 0;
		var _navigatorPages = 5;
		var _navigatorStart = -1;
		var _items;
		var _pageNumerHolders;
		var _firstTime = true;
		var _elementEvents;
		function _getTotalPages() {
			if (_divElement.ShowAllRecords) {
				return 1;
			}
			if (_jsonTable) {
				if (_itemCount > 0) {
					var d = 0;
					for (var i = 0; i < _jsonTable.Rows.length; i++) {
						if (_jsonTable.Rows[i].deleted) {
							d++;
						}
					}
					return Math.ceil((_jsonTable.Rows.length - d) / _itemCount);
				}
			}
			return 0
		}
		function _adjustItemHeight() {
			function adjIH(e) {
				for (var i = 0; i < e.children.length; i++) {
					if (e.children[i].style) {
						e.children[i].style.height = "";
						var o = e.children[i].style.overflow;
						e.children[i].style.overflow = 'scroll';
						e.children[i].scrollTop = 1;
						e.children[i].scrollTop = 0;
						e.children[i].style.height = e.children[i].scrollHeight + 'px';
						e.children[i].style.overflow = o;
						adjIH(e.children[i]);
					}
				}
			}
			for (var g = 0; g < _items.length; g++) {
				if (_items[g].style.display != 'none') {
					adjIH(_items[g]);
					_items[g].style.height = "";
					var o = _items[g].style.overflow;
					_items[g].style.overflow = 'scroll';
					_items[g].scrollTop = 1;
					_items[g].scrollTop = 0;
					_items[g].style.height = _items[g].scrollHeight + 'px';
					_items[g].style.overflow = o;
				}
			}
		}
		function _showPage() {
			function adjustDPpos(c) {
				if (c.getAttribute('useDP')) {
					var pid = c.getAttribute('useDPid');
					if (typeof pid == 'undefined' || pid == null || pid.length == 0) {
						var dpbut = document.getElementById('fd-but-' + c.id);
						if (dpbut) {
							dpbut.style.display = c.style.display;
						}
						JsonDataBinding.adjustDatePickerButtonPos(c.id);
					}
					else {
						var x = c.getAttribute('useDPx');
						var y = c.getAttribute('useDPy');
						if (x && y) {
							var dp = document.getElementById('fd-' + c.id);
							if (dp) {
								dp.style.top = y + 'px';
								dp.style.left = x + 'px';
							}
						}
						if (!c.dpset) {
							c.dpset = true;
							if (c.getAttribute('useDPv')) {
								var dpdiv = document.getElementById('fd-' + c.id);
								if (dpdiv) {
									dpdiv.style.display = 'none';
								}
							}
						}
					}
				}
				else {
					for (var i = 0; i < c.children.length; i++) {
						adjustDPpos(c.children[i]);
					}
				}
			}
			var pn = _getTotalPages();
			if (_pageIndex >= 0 && (_pageIndex < pn || pn == 0)) {
				var autoCR = typeof (_divElement.AutoColumnsAndRows) != 'undefined' && _divElement.AutoColumnsAndRows;
				var fillDirection = typeof (_divElement.ItemFillDirection) != 'undefined' ? _divElement.ItemFillDirection : 0;
				var tbl;
				var topNavigatorHeight = 0;
				if (autoCR) {
					for (var i = 0; i < _divElement.children.length; i++) {
						var tag = _divElement.children[i].tagName.toLowerCase();
						if (tag == 'table') {
							tbl = _divElement.children[i];
							break;
						}
					}
					if (typeof (_divElement.showTopNavigator) != 'undefined' && _divElement.showTopNavigator) {
						for (var i = 0; i < _divElement.children.length; i++) {
							var tag = _divElement.children[i].tagName.toLowerCase();
							if (tag == 'input') {
								topNavigatorHeight = _divElement.children[i].offsetHeight;
								break;
							}
						}
					}
				}
				var baseIndex = _pageIndex * _itemCount;
				var rDeleted = 0, r;
				for (r = 0; r < baseIndex; r++) {
					if (_jsonTable.Rows[r].deleted) {
						baseIndex++;
					}
				}
				var g;
				var dpParent = _divElement.parentNode;
				var parentWidth;
				var parentHeight;
				if (dpParent && dpParent.tagName && dpParent.tagName.toLowerCase() != 'body') {
					parentWidth = dpParent.offsetWidth;
					parentHeight = dpParent.offsetHeight;
				}
				if (!parentWidth || parentWidth == 0) {
					parentWidth = _divElement.parentwidth ? _divElement.parentwidth : (document.body.offsetWidth ? document.body.offsetWidth : _divElement.offsetWidth);//_divElement.offsetWidth;
				}
				if (!parentHeight || parentHeight == 0) {
					parentHeight = _divElement.parentheight ? _divElement.parentheight : (document.body.offsetHeight ? document.body.offsetHeight : 600); //_divElement.offsetHeight;
				}
				var colIdx = 0;
				var widthused = 0;
				var heightused = topNavigatorHeight;
				var lastheight = 0;
				var heightMax = topNavigatorHeight;
				for (g = 0; g < _items.length; g++) {
					r = baseIndex + g + rDeleted;
					while ((r >= 0 && r < _jsonTable.Rows.length) && _jsonTable.Rows[r].deleted) {
						rDeleted++;
						r = baseIndex + g + rDeleted;
					}
					if (r >= 0 && r < _jsonTable.Rows.length) {
						_jsonTable.rowIndex = r;
						_currentGroupIndex = g;
						JsonDataBinding.bindDataToElement(_items[g], _jsonTable.TableName, _firstTime);
						if (tbl)
							_items[g].style.display = 'block';
						else {
							if (!_divElement.displayStyle || _divElement.displayStyle == '') {
								_items[g].style.removeProperty('display');
							}
							else {
								_items[g].style.display = _divElement.displayStyle;//tbl ? 'block' : 'inline-block';
							}
						}
						_items[g].rowIndex = r;
						if (autoCR) {
							if (fillDirection == 0) {
								lastheight = _items[g].offsetHeight;
								if (widthused + _items[g].offsetWidth > parentWidth) {
									heightused += _items[g].offsetHeight;
									widthused = 0;
									heightMax = heightused;
								}
								_items[g].style.left = widthused + 'px';
								_items[g].style.top = heightused + 'px';
								widthused += _items[g].offsetWidth;
							}
							else {
								if (heightused + _items[g].offsetHeight > parentHeight) {
									widthused += _items[g].offsetWidth;
									heightused = topNavigatorHeight;
								}
								_items[g].style.left = widthused + 'px';
								_items[g].style.top = heightused + 'px';
								heightused += _items[g].offsetHeight;
								if (heightMax < heightused) {
									heightMax = heightused;
								}
							}
						}
						if (!_items[g].dpset) {
							_items[g].dpset = true;
							adjustDPpos(_items[g]);
						}
						_currentGroupIndex = g;
						if (_divElement.ondisplayItem) {
							_divElement.ondisplayItem();
						}
					}
					else {
						_items[g].style.display = 'none';
					}
				}
				if (_divElement.adjustItemHeight) {
					setTimeout(_adjustItemHeight, 0);
				}
				if (autoCR) {
					heightMax += lastheight;
					if(tbl) tbl.style.height = heightMax + 'px';
				}
				if (_pageNumerHolders) {
					var nStart = Math.floor(_pageIndex / _navigatorPages) * _navigatorPages;
					_navigatorStart = nStart;
					var nEnd = nStart + _navigatorPages;
					var sh = '';
					for (g = nStart; g < nEnd && g < pn; g++) {
						if (g == _pageIndex) {
							sh += "<span style='color:black;'>&nbsp;&nbsp;";
							sh += (g + 1);
							sh += "&nbsp;&nbsp;</span>";
						}
						else {
							sh += "<span style='cursor:pointer;' onclick='";
							sh += _divElement.id;
							sh += ".jsData.gotoPage(";
							sh += g;
							sh += ");'>&nbsp;&nbsp;";
							sh += (g + 1);
							sh += "&nbsp;&nbsp;</span>";
						}
					}
					if (g < pn) {
						if (g < pn - 1) {
							sh += " ...";
						}
						sh += " <span style='cursor:pointer;' onclick='";
						sh += _divElement.id;
						sh += ".jsData.gotoPage(";
						sh += (pn - 1);
						sh += ");'>&nbsp;&nbsp;";
						sh += (pn);
						sh += "&nbsp;&nbsp;</span>";
					}
					for (var i = 0; i < _pageNumerHolders.length; i++) {
						_pageNumerHolders[i].innerHTML = sh;
					}
				}
				if (typeof _divElement.onpageIndexChange == 'function') {
					_divElement.onpageIndexChange();
				}
			}
		}
		function adjustIds(e, idx) {
			if (e) {
				if (e.id) {
					e.id = e.id + '_' + idx;
				}
				if (e.children) {
					for (var i = 0; i < e.children.length; i++) {
						adjustIds(e.children[i], idx);
					}
				}
			}
		}
		function init() {
			function setDatePicker(v) {
				if (v.getAttribute('useDP')) {
					var pid = v.getAttribute('useDPid');
					if (typeof pid == 'undefined' || pid == null || pid.length == 0) {
						JsonDataBinding.CreateDatetimePickerForTextBox(v.id, v.getAttribute('useDPSize'), v.getAttribute('useDPTime'));
					}
					else if (pid == '*') {
						JsonDataBinding.CreateDatetimePickerForTextBox(v.id, v.getAttribute('useDPSize'), v.getAttribute('useDPTime'), true, null, v.getAttribute('useDPfix'));
					}
					else {
						JsonDataBinding.CreateDatetimePickerForTextBox(v.id, v.getAttribute('useDPSize'), v.getAttribute('useDPTime'), true, v.parentNode, v.getAttribute('useDPfix'));
					}
				}
				else {
					for (var k = 0; k < v.children.length; k++) {
						setDatePicker(v.children[k]);
					}
				}
			}
			var id, i;
			if (!_items) {
				//
				id = _divElement.id;
				_items = new Array();
				var el = _divElement.getElementsByTagName("div");
				for (i = 0; i < el.length; i++) {
					var nm = el[i].getAttribute('name');
					if (nm && nm == id) {
						_items.push(el[i]);
					}
				}
				_itemCount = _items.length;
				for (i = 0; i < _itemCount; i++) {
					setDatePicker(_items[i]);
				}
			}
			if (_divElement.ShowAllRecords) {
				if (_jsonTable) {
					if (_itemCount < _jsonTable.Rows.length) {
						id = _divElement.id;
						//clone more div to fill the page
						var divTemp;
						var tbl;
						for (i = 0; i < _divElement.children.length; i++) {
							var tag = _divElement.children[i].tagName.toLowerCase();
							if (tag == 'table') {
								tbl = _divElement.children[i];
							}
							else if (tag == 'div') {
								divTemp = _divElement.children[i];
							}
							if (tbl && divTemp) break;
						}
						if (tbl) {
							var tblBody = JsonDataBinding.getTableBody(tbl);
							//
							while (_itemCount < _jsonTable.Rows.length) {
								var tr = tblBody.insertRow(-1);
								for (i = 0; i < divElement.repeatColumnCount; i++) {
									var td = document.createElement('td');
									tr.appendChild(td);
									var divNew = divTemp.cloneNode(true);
									adjustIds(divNew, i + _itemCount);
									td.appendChild(divNew);
									_items.push(divNew);
									if (_elementEvents) {
										for (var k = 0; k < _elementEvents.length; k++) {
											if (_elementEvents[k].events) {
												var e0 = document.getElementById(_elementEvents[k].id);
												for (var m = 0; m < _elementEvents[k].events.length; m++) {
													activateEvent(e0, _elementEvents[k].id, i + _itemCount, _elementEvents[k].events[m]);
												}
											}
										}
									}
									setDatePicker(divNew);
								}
								_itemCount += _divElement.repeatColumnCount;
							}
						}
						else {
							while (_itemCount < _jsonTable.Rows.length) {
								for (i = 0; i < divElement.repeatColumnCount; i++) {
									var divNew = divTemp.cloneNode(true);
									adjustIds(divNew, i + _itemCount);
									_divElement.appendChild(divNew);
									_items.push(divNew);
									if (_elementEvents) {
										for (var k = 0; k < _elementEvents.length; k++) {
											if (_elementEvents[k].events) {
												var e0 = document.getElementById(_elementEvents[k].id);
												for (var m = 0; m < _elementEvents[k].events.length; m++) {
													activateEvent(e0, _elementEvents[k].id, i + _itemCount, _elementEvents[k].events[m]);
												}
											}
										}
									}
									setDatePicker(divNew);
								}
								_itemCount += _divElement.repeatColumnCount;
							}
						}
					}
				}
			}
			else {
				if (!_pageNumerHolders) {
					id = _divElement.id + '_sp';
					_pageNumerHolders = new Array();
					var sps = _divElement.getElementsByTagName("span");
					for (i = 0; i < sps.length; i++) {
						var nm = sps[i].getAttribute('name');
						if (nm == id) {
							_pageNumerHolders.push(sps[i]);
						}
					}
				}
			}
			//
			if (_jsonTable) {
				_pageIndex = 0;
				_firstTime = true;
				_showPage();
				_firstTime = false;
			}
		}
		function _onvaluechanged(tblname, r, c, val) {
			if (_jsonTable && _jsonTable.TableName == tblname) {
				//_showPage();
			}
		}
		//
		function _groupsPerPage() {
			return _itemCount;
		}
		function _onPageIndexChange(name) {
			//if (name == _jsonTable.TableName) {
			//	var r0 = _jsonTable.rowIndex;
			//}
		}
		function _onDataReady(jsData) {
			var diff = (_jsonTable != jsData);
			_jsonTable = jsData;
			init();
			if (diff) {
				_addvaluechangehandler(_jsonTable.TableName, obj);
			}
		}
		function _gotoPage(pageIndex) {
			var pn = _getTotalPages();
			_pageIndex = pageIndex;
			if (_pageIndex >= pn - 1) {
				_pageIndex = pn - 1;
			}
			if (_pageIndex < 0) {
				_pageIndex = 0;
			}
			if (_pageIndex < pn) {
				_showPage();
			}
		}
		function _goNextPage() {
			var pn = _getTotalPages();
			if (_pageIndex < pn - 1) {
				_pageIndex++;
				_gotoPage(_pageIndex);
			}
		}
		function _goPrevPage() {
			if (_pageIndex > 0) {
				_pageIndex--;
				_gotoPage(_pageIndex);
			}
		}
		function _gotoFirstPage() {
			if (_pageIndex != 0) {
				_gotoPage(0);
			}
		}
		function _gotoLastPage() {
			var pn = _getTotalPages();
			if (_pageIndex != pn - 1) {
				_gotoPage(pn - 1);
			}
		}
		function _setTableRowIndex() {
			if (_jsonTable && _items && _currentGroupIndex >= 0 && _currentGroupIndex < _items.length) {
				if (_items[_currentGroupIndex] && _items[_currentGroupIndex].rowIndex >= 0 && _items[_currentGroupIndex].rowIndex < _jsonTable.Rows.length) {
					JsonDataBinding.dataMoveToRecord(_jsonTable.TableName, _items[_currentGroupIndex].rowIndex);
				}
			}
		}
		function _setCurrentGroupIndex(index) {
			_currentGroupIndex = index;
		}
		function _getElement(id) {
			return document.getElementById(id + '_' + _currentGroupIndex);
		}
		function activateEvent(e0, id, g, evt) {
			var el = document.getElementById(id + '_' + g);
			el[evt] = (function(x) {
				return function(e) {
					_currentGroupIndex = x;
					_setTableRowIndex();
					e0[evt](e);
				};
			} (g));
		}
		function _attachElementEvent(id, evt) {
			if (_items) {
				var e0 = document.getElementById(id); //from template
				for (var g = 0; g < _items.length; g++) {
					activateEvent(e0, id, g, evt);
					//var el = document.getElementById(id + '_' + g);
					//el[evt] = (function(x) {
					//    return function(e) {
					//        _currentGroupIndex = x;
					//        _setTableRowIndex();
					//        e0[evt](e);
					//    };
					//} (g));
				}
			}
		}
		function _pushEvent(id, evt) {
			var i;
			if (!_elementEvents) _elementEvents = new Array();
			var e;
			for (i = 0; i < _elementEvents.length; i++) {
				if (_elementEvents[i].id == id) {
					e = _elementEvents[i];
					break;
				}
			}
			if (!e) {
				e = {};
				e.id = id;
				_elementEvents.push(e);
			}
			if (!e.events) e.events = new Array();
			for (i = 0; i < e.events.length; i++) {
				if (e.events[i] == evt) {
					return;
				}
			}
			e.events.push(evt);
		}
		function _getPageIndex() {
			return _pageIndex;
		}

		function _getCurrentGroupIndex() {
			return _currentGroupIndex;
		}
		function _setNavigatorPages(pnumber) {
			_navigatorPages = pnumber;
		}
		function _getNavigatorPages() {
			return _navigatorPages;
		}
		function _refreshDisplay() {
			_pageIndex = -1;
			_gotoFirstPage();
		}
		function _refreshCurrentPage() {
			_gotoPage(_pageIndex);
		}
		init();
		var obj = {
			refreshPage: function() {
				_showPage();
			},
			oncellvaluechange: function(tblname, r, c, val) {
				_onvaluechanged(tblname, r, c, val);
			},
			onDataReady: function(jsData) {
				_onDataReady(jsData);
			},
			onPageIndexChange: function(name) {
				_onPageIndexChange(name);
			},
			groupsPerPage: function() {
				return _groupsPerPage();
			},
			getPageIndex: function() {
				return _getPageIndex();
			},
			getTotalPages: function() {
				return _getTotalPages();
			},
			gotoNextPage: function() {
				_goNextPage();
			},
			gotoPrevPage: function() {
				_goPrevPage();
			},
			gotoFirstPage: function() {
				_gotoFirstPage();
			},
			gotoLastPage: function() {
				_gotoLastPage();
			},
			gotoPage: function(pageIndex) {
				_gotoPage(pageIndex);
			},
			setCurrentGroupIndex: function(index) {
				_setCurrentGroupIndex(index);
			},
			getCurrentGroupIndex: function() {
				return _getCurrentGroupIndex();
			},
			setNavigatorPages: function(pnumber) {
				_setNavigatorPages(pnumber - 1);
			},
			getNavigatorPages: function() {
				_getNavigatorPages();
			},
			getElement: function(id) {
				return _getElement(id);
			},
			attachElementEvent: function(id, evt) {
				_pushEvent(id, evt);
				_attachElementEvent(id, evt);
			},
			refreshDisplay: function() {
				_refreshDisplay();
			},
			refreshCurrentPage: function() {
				_refreshCurrentPage();
			}
		}
		if (jsTable) {
			_addvaluechangehandler(jsTable.TableName, obj);
		}
		return obj;
	},
	//===end of data repeater
	datetime: {
		isValidDate: function(d) {
			if (typeof d != 'undefined' && d != null) {
				if (d instanceof Date) {
					return !isNaN(d.getTime());
				}
			}
			return false;
		},
		toDate: function(d) {
			if (typeof d != 'undefined' && d != null) {
				var d0;
				if (JsonDataBinding.datetime.isValidDate(d)) {
					d0 = d;
				}
				else {
					d0 = new Date(d);
					if (!JsonDataBinding.datetime.isValidDate(d0)) {
						d0 = JsonDataBinding.datetime.parseIso(d);
						if (!JsonDataBinding.datetime.isValidDate(d0)) {
							return null;
						}
					}
				}
				return d0;
			}
			return null;
		},
		toDate0: function(d) {
			var d2 = JsonDataBinding.datetime.toDate(d);
			if (typeof d2 == 'undefined' || d2 == null) {
				return new Date(0, 0, 0, 0, 0, 0, 0);
			}
			return d2;
		},
		setDate: function(d, n) {
			d.setDate(n);
			return d;
		},
		setMonth: function(d, n) {
			d.setMonth(n);
			return d;
		},
		setFullYear: function(d, n) {
			d.setFullYear(n);
			return d;
		},
		setHours: function(d, n) {
			d.setHours(n);
			return d;
		},
		setMinutes: function(d, n) {
			d.setMinutes(n);
			return d;
		},
		setSeconds: function(d, n) {
			d.setSeconds(n);
			return d;
		},
		setMilliseconds: function(d, n) {
			d.setMilliseconds(n);
			return d;
		},
		setTime: function(d, n) {
			d.setTime(n);
			return d;
		},
		setUTCDate: function(d, n) {
			d.setUTCDate(n);
			return d;
		},
		setUTCMonth: function(d, n) {
			d.setUTCMonth(n);
			return d;
		},
		setUTCFullYear: function(d, n) {
			d.setUTCFullYear(n);
			return d;
		},
		setUTCHours: function(d, n) {
			d.setUTCHours(n);
			return d;
		},
		setUTCMinutes: function(d, n) {
			d.setUTCMinutes(n);
			return d;
		},
		setUTCSeconds: function(d, n) {
			d.setUTCSeconds(n);
			return d;
		},
		setUTCMilliseconds: function(d, n) {
			d.setUTCMilliseconds(n);
			return d;
		},
		addMilliseconds: function(d, milliseconds) {
			var d2 = JsonDataBinding.datetime.toDate(d);
			if (d2 instanceof Date) {
				d2.setMilliseconds(d.getMilliseconds() + milliseconds);
				return d2;
			}
		},
		addSeconds: function(d, seconds) {
			var d2 = JsonDataBinding.datetime.toDate(d);
			if (d2 instanceof Date) {
				d2.setSeconds(d.getSeconds() + seconds);
				return d2;
			}
		},
		addMinutes: function(d, minutes) {
			var d2 = JsonDataBinding.datetime.toDate(d);
			if (d2 instanceof Date) {
				d2.setMinutes(d.getMinutes() + minutes);
				return d2;
			}
		},
		addHours: function(d, hours) {
			var d2 = JsonDataBinding.datetime.toDate(d);
			if (d2 instanceof Date) {
				d2.setHours(d.getHours() + hours);
				return d2;
			}
		},
		addDays: function(d, days) {
			var d2 = JsonDataBinding.datetime.toDate(d);
			if (d2 instanceof Date) {
				d2.setDate(d.getDate() + days);
				return d2;
			}
		},
		setValue: function(d, yr, mo, dy, hr, mi, se, ml) {
			d.setFullYear(yr);
			d.setMonth(mo);
			d.setDate(dy);
			d.setHours(hr);
			d.setMinutes(mi);
			d.setSeconds(se);
			d.setMilliseconds(ml);
			return d;
		},
		setUTCValue: function(d, yr, mo, dy, hr, mi, se, ml) {
			d.setUTCFullYear(yr);
			d.setUTCMonth(mo);
			d.setUTCDate(dy);
			d.setUTCHours(hr);
			d.setUTCMinutes(mi);
			d.setUTCSeconds(se);
			d.setUTCMilliseconds(ml);
			return d;
		},
		parseIso: function(iso) {
			if (iso) {
				var regexp = "([0-9]{4})(-([0-9]{2})(-([0-9]{2})" +
				  "([T]|[ ]([0-9]{2}):([0-9]{2})(:([0-9]{2})(\.([0-9]+))?)?" +
				  "(Z|(([-+])([0-9]{2}):([0-9]{2})))?)?)?)?";
				var d = iso.match(new RegExp(regexp));
				if (typeof d != 'undefined' && d != null) {
					if (d.length > 1) {
						var date = new Date(d[1], 0, 1);
						if (d[3]) { date.setMonth(d[3] - 1); }
						if (d[5]) { date.setDate(d[5]); }
						if (d[7]) { date.setHours(d[7]); }
						if (d[8]) { date.setMinutes(d[8]); }
						if (d[10]) { date.setSeconds(d[10]); }
						if (d[12]) { date.setMilliseconds(Number("0." + d[12]) * 1000); }
						return date;
					}
				}
			}
		},
		parseIsoUTC: function(iso) {
			if (iso) {
				var regexp = "([0-9]{4})(-([0-9]{2})(-([0-9]{2})" +
				  "([T]|[ ]([0-9]{2}):([0-9]{2})(:([0-9]{2})(\.([0-9]+))?)?" +
				  "(Z|(([-+])([0-9]{2}):([0-9]{2})))?)?)?)?";
				var d = iso.match(new RegExp(regexp));
				if (typeof d != 'undefined' && d != null) {
					if (d.length > 1) {
						var offset = 0;
						var date = new Date(d[1], 0, 1);
						if (d[3]) { date.setMonth(d[3] - 1); }
						if (d[5]) { date.setDate(d[5]); }
						if (d[7]) { date.setHours(d[7]); }
						if (d[8]) { date.setMinutes(d[8]); }
						if (d[10]) { date.setSeconds(d[10]); }
						if (d[12]) { date.setMilliseconds(Number("0." + d[12]) * 1000); }
						if (d[14]) {
							offset = (Number(d[16]) * 60) + Number(d[17]);
							offset *= ((d[15] == '-') ? 1 : -1);
						}
						offset -= date.getTimezoneOffset();
						return new Date((Number(date) + (offset * 60 * 1000)));
					}
				}
			}
		},
		toIso: function(d0) {
			if (typeof d0 != 'undefined' && d0 != null) {
				var d = JsonDataBinding.datetime.toDate(d0);
				if (d instanceof Date) {
					var mo = d.getMonth() + 1;
					var dy = d.getDate();
					var hh = d.getHours();
					var mi = d.getMinutes();
					var s = d.getSeconds();
					return ''.concat(d.getFullYear(), '-', (mo > 9 ? mo : '0' + mo), '-', (dy > 9 ? dy : '0' + dy), ' ', (hh > 9 ? hh : '0' + hh), ':', (mi > 9 ? mi : '0' + mi), ':', (s > 9 ? s : '0' + s));
				}
			}
		},
		toIsoUTC: function(d0) {
			if (typeof d0 != 'undefined' && d0 != null) {
				var d = JsonDataBinding.datetime.toDate(d0);
				if (d instanceof Date) {
					var mo = d.getUTCMonth() + 1;
					var dy = d.getUTCDate();
					var hh = d.getUTCHours();
					var mi = d.getUTCMinutes();
					var s = d.getUTCSeconds();
					return ''.concat(d.getUTCFullYear(), '-', (mo > 9 ? mo : '0' + mo), '-', (dy > 9 ? dy : '0' + dy), ' ', (hh > 9 ? hh : '0' + hh), ':', (mi > 9 ? mi : '0' + mi), ':', (s > 9 ? s : '0' + s));
				}
			}
		},
		toLocalDate: function(d0) {
			if (typeof d0 != 'undefined' && d0 != null) {
				var d = JsonDataBinding.datetime.toDate(d0);
				if (d instanceof Date) {
					var d2 = new Date();
					d2.setUTCFullYear(d.getFullYear(), d.getMonth(), d.getDate());
					d2.setUTCHours(d.getHours(), d.getMinutes(), d.getSeconds(), d.getMilliseconds());
					return d2;
				}
			}
			return d0;
		},
		isValid: function(d) {
			if (Object.prototype.toString.call(d) !== "[object Date]")
				return false;
			return !isNaN(d.getTime());
		},
		getTimespan: function(dstart, dend) {
			var ts = new JsonDataBinding.timespan();
			ts.setWholeTimeByMilliseconds(dend - dstart);
			return ts;
		}
	},
	//===end of datetime
	/*
	add*(?) - the value can be larger than the unit; the value is rounded up; it goes up the units
	set*(?) - the value can be larger then the unit; the decimal part of the value goes down the units; it goes up the units from milliseconds
	setwhole*(?) -- same as set*(?)
	*/
	timespan: function(tsdays, tshours, tsminutes, tsseconds, tsmilliseconds) {
		var _wholemilliseconds = 0; //can be decimal
		//components, all integers
		var _days = 0;
		var _hours = 0;
		var _minutes = 0;
		var _seconds = 0;
		var _milliseconds = 0;
		var day2mis = 86400000; //1 day to milliseconds
		function intToDisp(s) {
			s = Math.abs(s);
			if (s < 10)
				return '0' + s;
			return '' + s;
		}
		function _isNegative() {
			return _wholemilliseconds < 0;
		}
		function _setSign(isPositive) {
			if (isPositive) {
				if (_wholemilliseconds < 0) {
					_wholemilliseconds = -_wholemilliseconds;
					_onchange();
				}
			}
			else {
				if (_wholemilliseconds > 0) {
					_wholemilliseconds = -_wholemilliseconds;
					_onchange();
				}
			}
		}
		function _onchange() {
			if (_wholemilliseconds == 0) {
				_milliseconds = 0;
				_seconds = 0;
				_minutes = 0;
				_hours = 0;
				_days = 0;
			}
			else if (_wholemilliseconds > 0) {
				_milliseconds = Math.floor(_wholemilliseconds % 1000);  //mis component
				var d = _wholemilliseconds / 1000.0;  //whole seconds
				_seconds = Math.floor(d % 60); //second component
				d = d / 60.0; //whole minutes
				_minutes = Math.floor(d % 60); //minute component
				d = d / 60.0;  //whole hours
				_hours = Math.floor(d % 24);   //hour component
				d = d / 24.0;
				_days = Math.floor(d); //day component
			}
			else {
				_milliseconds = Math.ceil(_wholemilliseconds % 1000);  //mis component
				var d = _wholemilliseconds / 1000.0;  //whole seconds
				_seconds = Math.ceil(d % 60); //second component
				d = d / 60.0; //whole minutes
				_minutes = Math.ceil(d % 60); //minute component
				d = d / 60.0;  //whole hours
				_hours = Math.ceil(d % 24);   //hour component
				d = d / 24.0;
				_days = Math.ceil(d); //day component
			}
		}
		function _addmilli(pmi) {
			_wholemilliseconds = _wholemilliseconds + pmi;
			_onchange();
		}
		function _setmilli(pmi) {
			_wholemilliseconds = 0;
			_addmilli(pmi);
		}
		function _addDays(pdays) {
			pdays = parseFloat(pdays);
			if (typeof pdays == "number" && !isNaN(pdays)) {
				_addmilli(day2mis * pdays);
			}
		}
		function _addHours(phours) {
			phours = parseFloat(phours);
			if (typeof phours == "number" && !isNaN(phours)) {
				_addmilli(3600000 * phours);
			}
		}
		function _addMinutes(pminutes) {
			pminutes = parseFloat(pminutes);
			if (typeof pminutes == "number" && !isNaN(pminutes)) {
				_addmilli(60000 * pminutes);
			}
		}
		function _addSeconds(pseconds) {
			pseconds = parseFloat(pseconds);
			if (typeof pseconds == "number" && !isNaN(pseconds)) {
				_addmilli(1000 * pseconds);
			}
		}
		function _addMilliseconds(pmilliseconds) {
			pmilliseconds = parseFloat(pmilliseconds);
			if (typeof pmilliseconds == "number" && !isNaN(pmilliseconds)) {
				_addmilli(pmilliseconds);
			}
		}
		function _addTimeSpan(time) {
			if (typeof time != 'undefined' && typeof time.wholeMillisecondsDecimal == 'function') {
				_wholemilliseconds = _wholemilliseconds + time.wholeMillisecondsDecimal();
				_onchange();
			}
		}
		function _setWholeTimeByMilliseconds(pmis) {
			pmis = parseFloat(pmis);
			if (typeof pmis != 'undefined' && !isNaN(pmis)) {
				_wholemilliseconds = pmis;
				_onchange();
			}
		}
		function _differenceInTimeSpan(starttime) {
			var ts = new JsonDataBinding.timespan();
			if (typeof starttime != 'undefined' && typeof starttime.wholeMillisecondsDecimal == 'function') {
				ts.setWholeTimeByMilliseconds(_wholemilliseconds - starttime.wholeMillisecondsDecimal());
			}
			else
				ts.setWholeTimeByMilliseconds(_wholemilliseconds);
			return ts;
		}
		function _setValues(pdays, phours, pminutes, psecs, pmis) {
			_wholemilliseconds = 0;
			pdays = parseFloat(pdays);
			phours = parseFloat(phours);
			pminutes = parseFloat(pminutes);
			psecs = parseFloat(psecs);
			pmis = parseFloat(pmis);
			if (typeof pdays != 'undefined' && !isNaN(pdays)) _wholemilliseconds += (day2mis * pdays);
			if (typeof phours != 'undefined' && !isNaN(phours)) _wholemilliseconds += (3600000 * phours);
			if (typeof pminutes != 'undefined' && !isNaN(pminutes)) _wholemilliseconds += (60000 * pminutes);
			if (typeof psecs != 'undefined' && !isNaN(psecs)) _wholemilliseconds += (1000 * psecs);
			if (typeof pmis != 'undefined' && !isNaN(pmis)) _wholemilliseconds += pmis;
			_onchange();
		}
		function _parseIsoString(days, time) {
			if (typeof time == 'string') {
				time = time.trim();
				if (time.length > 0) {
					var hours = 0, minutes = 0, seconds = 0, milliseconds = 0;
					var ss = time.split(':');
					var h = parseInt(ss[0]);
					if (!isNaN(h)) { hours = h; }
					if (ss.length > 1) {
						var mi = parseInt(ss[1]);
						if (!isNaN(mi)) { minutes = mi; }
						if (ss.length > 2) {
							var ses;
							var p = ss[2].indexOf('.');
							if (p >= 0) {
								ses = ss[2].substr(0, p);
								seconds = parseInt(ses);
								milliseconds = parseInt(ss[2].substr(p + 1));
							}
							else {
								seconds = parseInt(ss[2]);
							}
						}
					}
					var mis;
					mis = milliseconds + 1000 * (seconds + 60 * (minutes + 60 * (Math.abs(hours) + 24 * Math.abs(days))));
					if (days == 0) {
						if (hours < 0)
							mis = -mis;
					}
					else {
						if (days < 0) {
							mis = -mis;
						}
					}
					_wholemilliseconds = mis;
					_onchange();
				}
			}
		}
		var ret = {
			isNegative: function() { return _isNegative(); },
			days: function() { return _days; },
			hours: function() { return _hours; },
			minutes: function() { return _minutes; },
			seconds: function() { return _seconds; },
			milliseconds: function() { return _milliseconds; },
			wholeMilliseconds: function() {
				return Math.floor(_wholemilliseconds);
			},
			wholeMillisecondsDecimal: function() {
				return _wholemilliseconds;
			},
			wholeSeconds: function() {
				return Math.floor(_wholemilliseconds / 1000);
			},
			wholeSecondsDecimal: function() {
				return _wholemilliseconds / 1000.0;
			},
			wholeMinutes: function() {
				return Math.floor((_wholemilliseconds / 1000) / 60);
			},
			wholeMinutesDecimal: function() {
				return (_wholemilliseconds / 1000.0) / 60.0;
			},
			wholeHours: function() {
				return Math.floor(((_wholemilliseconds / 1000) / 60) / 60);
			},
			wholeHoursDecimal: function() {
				return ((_wholemilliseconds / 1000.0) / 60.0) / 60.0;
			},
			wholeDays: function() {
				return Math.floor((((_wholemilliseconds / 1000) / 60) / 60) / 24);
			},
			wholeDaysDecimal: function() {
				return (((_wholemilliseconds / 1000.0) / 60.0) / 60.0) / 24.0;
			},
			toWholeString: function() {
				return (_isNegative() ? '-' : '') + Math.abs(_days) + ' ' + intToDisp(_hours) + ':' + intToDisp(_minutes) + ':' + intToDisp(_seconds) + (_milliseconds != 0 ? '.' + Math.abs(_milliseconds) : '');
			},
			toTimeString: function() {
				return (_isNegative() ? '-' : '') + intToDisp(Math.abs(_hours) + 24 * Math.abs(_days)) + ':' + intToDisp(_minutes) + ':' + intToDisp(_seconds) + (_milliseconds != 0 ? '.' + Math.abs(_milliseconds) : '');
			},
			toShortTimeString: function() {
				return (_isNegative() ? '-' : '') + intToDisp(Math.abs(_hours) + 24 * Math.abs(_days)) + ':' + intToDisp(_minutes) + ':' + intToDisp(_seconds);
			},
			addMilliseconds: function(pmilliseconds) {
				_addMilliseconds(pmilliseconds);
			},
			addSeconds: function(pseconds) {
				_addSeconds(pseconds);
			},
			addMinutes: function(pminutes) {
				_addMinutes(pminutes);
			},
			addHours: function(phours) {
				_addHours(phours);
			},
			addDays: function(pdays) {
				_addDays(pdays);
			},
			addTimeSpan: function(time) {
				_addTimeSpan(time);
			},
			//hh:mm:ss.mmm
			parseIsoString: function(time) {
				_parseIsoString(0, time);
			},
			//days hh:mm:ss.mmm
			parseTimeSpan: function(time) {
				if (typeof time == 'string') {
					var d = 0;
					var p = time.indexOf(' ');
					if (p > 0) {
						d = parseInt(time.substr(0, p));
						time = time.substr(p + 1);
					}
					_parseIsoString(d, time);
				}
			},
			equalExact: function(time) {
				if (typeof time != 'undefined' && typeof time.wholeMillisecondsDecimal == 'function') {
					return _wholemilliseconds == time.wholeMillisecondsDecimal();
				}
				return false;
			},
			equal: function(time) {
				if (typeof time != 'undefined' && typeof time.wholeMilliseconds == 'function') {
					return Math.floor(_wholemilliseconds) == time.wholeMilliseconds();
				}
				return false;
			},
			differenceInTimeSpan: function(time) {
				return _differenceInTimeSpan(time);
			},
			differenceInMilliseconds: function(time) {
				var ts = _differenceInTimeSpan(time);
				if (ts) {
					return ts.wholeMillisecondsDecimal();
				}
			},
			differenceInSeconds: function(time) {
				var ts = _differenceInTimeSpan(time);
				if (ts) {
					return ts.wholeSecondsDecimal();
				}
			},
			differenceInMinutes: function(time) {
				var ts = _differenceInTimeSpan(time);
				if (ts) {
					return ts.wholeMinutesDecimal();
				}
			},
			differenceInHours: function(time) {
				var ts = _differenceInTimeSpan(time);
				if (ts) {
					return ts.wholeHoursDecimal();
				}
			},
			differenceInDays: function(time) {
				var ts = _differenceInTimeSpan(time);
				if (ts) {
					return ts.wholeDaysDecimal();
				}
			},
			setTimeSpan: function(time) {
				if (typeof time != 'undefined' && typeof time.wholeMillisecondsDecimal == 'function') {
					_setWholeTimeByMilliseconds(time.wholeMillisecondsDecimal());
				}
			},
			setValues: function(pdays, phours, pminutes, psecs, pmis) {
				_setValues(pdays, phours, pminutes, psecs, pmis);
			},
			setDays: function(pdays) {
				pdays = parseFloat(pdays);
				if (typeof pdays != 'undefined' && !isNaN(pdays)) {
					var c = _days * day2mis;
					var e = Math.floor(parseFloat(pdays)) * day2mis;
					_wholemilliseconds = _wholemilliseconds + e - c;
					_onchange();
				}
			},
			setHours: function(phours) {
				phours = parseFloat(phours);
				if (typeof phours != 'undefined' && !isNaN(phours)) {
					var c = _hours * 3600000;
					var e = Math.floor(parseFloat(phours)) * 3600000;
					_wholemilliseconds = _wholemilliseconds + e - c;
					_onchange();
				}
			},
			setMinutes: function(pminutes) {
				pminutes = parseFloat(pminutes);
				if (typeof pminutes != 'undefined' && !isNaN(pminutes)) {
					var c = _minutes * 60000;
					var e = Math.floor(parseFloat(pminutes)) * 60000;
					_wholemilliseconds = _wholemilliseconds + e - c;
					_onchange();
				}
			},
			setSeconds: function(psecs) {
				psecs = parseFloat(psecs);
				if (typeof psecs != 'undefined' && !isNaN(psecs)) {
					var c = _seconds * 1000;
					var e = Math.floor(parseFloat(psecs)) * 1000;
					_wholemilliseconds = _wholemilliseconds + e - c;
					_onchange();
				}
			},
			setMilliseconds: function(pmis) {
				pmis = parseFloat(pmis);
				if (typeof pmis != 'undefined' && !isNaN(pmis)) {
					var c = _milliseconds;
					var e = Math.floor(parseFloat(pmis));
					_wholemilliseconds = _wholemilliseconds + e - c;
					_onchange();
				}
			},
			setWholeTimeByMilliseconds: function(pmis) {
				_setWholeTimeByMilliseconds(pmis);
			},
			setWholeTimeBySeconds: function(psecs) {
				psecs = parseFloat(psecs);
				if (typeof psecs != 'undefined' && !isNaN(psecs)) {
					_setWholeTimeByMilliseconds(psecs * 1000);
				}
			},
			setWholeTimeByMinutes: function(pmins) {
				pmins = parseFloat(pmins);
				if (typeof pmins != 'undefined' && !isNaN(pmins)) {
					_setWholeTimeByMilliseconds(pmins * 60000);
				}
			},
			setWholeTimeByHours: function(phours) {
				phours = parseFloat(phours);
				if (typeof phours != 'undefined' && !isNaN(phours)) {
					_setWholeTimeByMilliseconds(phours * 3600000);
				}
			},
			setWholeTimeByDays: function(pdays) {
				pdays = parseFloat(pdays);
				if (typeof pdays != 'undefined' && !isNaN(pdays)) {
					_setWholeTimeByMilliseconds(pdays * day2mis);
				}
			},
			setWholeTimeByDates: function(start, end) {
				var mis = end - start;
				if (typeof mis != 'undefined' && !isNaN(mis)) {
					_setWholeTimeByMilliseconds(mis);
				}
			}
		};
		if (typeof tsdays != "undefined") {
			if (typeof tshours == 'undefined') {
				if (typeof tsdays == "number") {
					_setWholeTimeByMilliseconds(tsdays * day2mis);
				}
				else if (typeof tsdays.wholeMillisecondsDecimal == 'function') {
					_setWholeTimeByMilliseconds(tsdays.wholeMillisecondsDecimal());
				}
			}
			else {
				_setValues(tsdays, tshours, tsminutes, tsseconds, tsmilliseconds);
			}
		}
		return ret;
	},
	CreateComboBox: function(parentElement, values, initValue, initWidth, fillParent, textSearch) {
		var _img, _input;
		var _div = document.createElement('div');
		parentElement.appendChild(_div);
		//parentElement.style.position = 'relative';
		_div.style.position = 'relative';
		_div.style.display = 'inline';
		//
		var tbl = document.createElement('table');
		tbl.cellPadding = 0;
		tbl.cellSpacing = 0;
		tbl.border = 0;
		tbl.style.margin = 0;
		var tblBody = JsonDataBinding.getTableBody(tbl);
		var tr = tblBody.insertRow(-1);
		var td = document.createElement('td');
		tr.appendChild(td);
		//
		_input = document.createElement('input');
		_input.type = 'text';
		_input.setAttribute('autocomplete', 'off');
		_input.value = initValue;
		_input.style.width = '100%';
		//
		td.appendChild(_input);
		td.style.width = '100%';
		//
		var td0 = document.createElement('td');
		if (JsonDataBinding.IsFireFox() || JsonDataBinding.IsOpera()) {
			td0.innerHTML = '&nbsp;&nbsp;';
		}
		else {
			td0.innerHTML = '&nbsp;';
		}
		tr.appendChild(td0);
		//
		var td2 = document.createElement('td');
		tr.appendChild(td2);
		_img = document.createElement('img');
		_img.src = 'libjs/downArrow_t16.png';
		_img.style.cursor = 'pointer';
		//
		td2.appendChild(_img);
		//
		_div.appendChild(tbl);
		//
		var _select = document.createElement('select');
		_select.style.display = 'none';
		_select.style.position = 'absolute';
		_select.size = values.length;
		if (_select.size > 30) {
			_select.style.height = '180px';
			_select.style.overflowY = 'scroll';
		}
		else {
			_select.style.height = 'auto';
			_select.style.overflowY = '';
		}
		document.body.appendChild(_select);
		//====================================
		for (var i = 0; i < values.length; i++) {
			var o = document.createElement('option');
			o.text = values[i].text;
			o.value = values[i].value;
			try {
				_select.add(o, null); // standards compliant; doesn't work in IE
			}
			catch (ex) {
				_select.add(o); // IE only
			}
			if (initValue == values[i].value) {
				_select.selectedIndex = _select.options.length - 1;
			}
		}
		//called by down arrow
		function showDropDown() {
			var zi = JsonDataBinding.getPageZIndex(_div);
			_div.style.zIndex = zi + 1;
			var pos = JsonDataBinding.ElementPosition.getElementPosition(_input);
			_select.style.zIndex = zi + 1;
			_select.style.top = (pos.y + 20) + 'px';
			_select.style.left = pos.x + 'px';
			_select.style.display = 'block';
			_select.focus();
		}
		function ontxtchange() {
			if (_div.jsData) {
				if (_div.jsData.onchange) {
					_div.jsData.onchange({ target: _div.jsData });
				}
				else if (_div.jsData.change) {
					_div.jsData.change({ target: _div.jsData });
				}
			}
			return true;
		}
		function comboselect_onchange() {
			if (_select.selectedIndex != -1) {
				if (_div.jsData.setter) {
					_div.jsData.setter(_select.options[_select.selectedIndex].value, _input);
				}
				else {
					_input.value = _select.options[_select.selectedIndex].value;
				}
				ontxtchange();
			}
			return true;
		}
		function txtkeydown(e) {
			e = window.event || e;
			var keyCode = e ? e.keyCode : 0;
			if (keyCode == 40 || keyCode == 38) {
				showDropDown();
				comboselect_onchange();
			}
			else if (keyCode == 13) {
				e.cancelBubble = true;
				if (e.returnValue) e.returnValue = false;
				if (e.stopPropagation) e.stopPropagation();
				comboselect_onchange();
				_select.style.display = 'none';
				_input.focus();
				return false;
			}
			else if (keyCode == 9) return true;
			else {
				var itemfound = false;
				if (textSearch && keyCode) {
					_select.style.display = 'block';
					showDropDown();
					var c = String.fromCharCode(keyCode);
					c = c.toUpperCase();
					toFind = _input.value.toUpperCase() + c;
					for (i = 0; i < _select.options.length; i++) {
						nextOptionText = _select.options[i].text.toUpperCase();
						if (toFind == nextOptionText) {
							_select.selectedIndex = i;
							itemfound = true;
							break;
						}
						if (i < _select.options.length - 1) {
							lookAheadOptionText = _select.options[i + 1].text.toUpperCase();
							if ((toFind > nextOptionText) &&
								(toFind < lookAheadOptionText)) {
								_select.selectedIndex = i + 1;
								itemfound = true;
								break;
							}
						}
						else {
							if (toFind > nextOptionText) {
								_select.selectedIndex = _select.options.length - 1; // stick it at the end
								itemfound = true;
								break;
							}
						}
					}
				}
				if (!itemfound) {
					if (_input.onhandleKeydown) {
						_input.onhandleKeydown();
					}
				}
			}
			return true;
		}
		function comboselect_imageClick() {
			showDropDown();
			return true;
		}
		function comboselect_blur() {
			_select.style.display = 'none';
		}
		function comboselect_onkeyup(e) {
			e = window.event || e;
			if (e) {
				var keyCode = e.keyCode;
				if (keyCode) {
					if (keyCode == 13) {
						comboselect_onchange();
						_select.style.display = 'none';
						_input.focus();
					}
				}
			}
			return true;
		}
		function document_click(e) {
			var sender = JsonDataBinding.getSender(e);
			if (sender != _img) {
				_select.style.display = 'none';
			}
			return true;
		}
		function _setWidth(width) {
			var w = (_img.offsetWidth ? _img.offsetWidth : 20) + 20;
			if (width > w) {
				_input.style.width = (width - w - 1) + 'px';
			}
		}
		function _adjustPosition() {
		}
		function _init() {
		}
		//
		JsonDataBinding.AttachEvent(_input, 'onkeydown', txtkeydown);
		JsonDataBinding.AttachEvent(_input, 'onchange', ontxtchange);
		JsonDataBinding.AttachEvent(_img, 'onclick', comboselect_imageClick);
		JsonDataBinding.AttachEvent(_select, 'onblur', comboselect_blur);
		JsonDataBinding.AttachEvent(_select, 'onchange', comboselect_onchange);
		JsonDataBinding.AttachEvent(_select, 'onkeyup', comboselect_onkeyup);
		JsonDataBinding.AttachEvent(document, 'onclick', document_click);
		if (initWidth) {
			_setWidth(initWidth);
		}
		_div.jsData = {
			div: _div,
			select: _select,
			input: _input,
			setWidth: function(width) { _setWidth(width); },
			getParentNode: function() { return _div.parentNode; },
			getValue: function() { return _input.value; },
			init: function() { _init(); },
			adjustPosition: function() {
				_adjustPosition();
			},
			applyTextChanges: function () {
				ontxtchange();
			},
			getInput: function () {
				return _input;
			}
		}
		return _div.jsData;
	},
	//===end of combobox
	//===start of sendkeys===
	initSendKeys: function() {
		_initSendKeys();
	},
	sendKeys: function(keys) {
		_sendKeys(keys);
	},
	selectNextInput: function() {
		_selectNextInput();
	},
	//===end of sendkeys===
	//===start of mouse event===
	MousehButton: function(event) {
		if ('which' in event) {
			switch (event.which) {
				case 1:
					return 1; //alert ("Left mouse button was pressed");
					//break;
				case 2:
					return 4; //alert ("Middle mouse button was pressed");
					//break;
				case 3:
					return 2; //alert ("Right mouse button was pressed");
					//break;
			}
		}
		else {
			// IE before version 9
			if ('button' in event) {
				var buttons = 0;
				if (event.button & 1) {
					buttons += 1; //"left";
				}
				if (event.button & 2) {
					//if (buttons=="")
					//{buttons+="right";}
					//else
					//{buttons+=" + right";}
					buttons += 2;
				}
				if (event.button & 4) {
					//if (buttons=="")
					//  {buttons+="middle";}
					//else
					//  {buttons+=" + middle";}
					buttons += 4;
				}
				//alert ("The following button was pressed: " + buttons);
				return buttons;
			}
		}
		return 0;
	},
	//===end of mouse event===
	//===start of generic utilities===
	setOpacity: function(obj, opacityValue) {
		if (opacityValue < 0) opacityValue = 0;
		if (opacityValue > 100) opacityValue = 100;
		obj.style.opacity = (opacityValue / 100);
		obj.style.MozOpacity = (opacityValue / 100);
		obj.style.KhtmlOpacity = (opacityValue / 100);
		obj.style.filter = 'alpha(opacity=' + opacityValue + ')';
	},
	getOpacity: function(obj) {
		if (obj.style) {
			if (typeof (obj.style.opacity) != 'undefined' && (obj.style.opacity || obj.style.opacity === 0)) {
				return obj.style.opacity * 100;
			}
			if (typeof (obj.style.MozOpacity) != 'undefined' && (obj.style.MozOpacity || obj.style.MozOpacity === 0)) {
				return obj.style.MozOpacity * 100;
			}
			if (typeof (obj.style.KhtmlOpacity) != 'undefined' && (obj.style.KhtmlOpacity || obj.style.KhtmlOpacity === 0)) {
				return obj.style.KhtmlOpacity * 100;
			}
			if (typeof (obj.style.filter) != 'undefined' && obj.style.filter != null) {
				var pos = obj.style.filter.indexOf('=');
				if (pos > 0) {
					var s = obj.style.filter.substr(pos + 1);
					if (s && s.length > 0) {
						if (s[s.length - 1] == ')') {
							s = s.substr(0, s.length - 1);
						}
						return parseInt(s);
					}
				}
			}
		}
		return 100;
	},
	setBoxShadow: function(obj, shadow) {
		obj.style.boxShadow = shadow;
		obj.style.mozBoxShadow = shadow;
		obj.style.webkitBoxShadow = shadow;
	},
	getBoxShadow: function(obj) {
		return obj.style.boxShadow ? obj.style.boxShadow : (obj.style.webkitBoxShadow ? obj.style.webkitBoxShadow : obj.style.mozBoxShadow);
	},
	setButtonImage: function(bt, imgUrl, imgWidth, imgHeight) {
		bt.style.backgroundImage = "url('" + imgUrl + "')";
		//bt.style.backgroundColor = 'transparent';
		bt.style.backgroundRepeat = 'no-repeat';

		var h = (bt.offsetHeight - imgHeight) / 2;
		if (h < 0) h = 0;
		bt.style.backgroundPosition = '2px ' + h + 'px';
		bt.style.paddingLeft = (imgWidth + 2) + 'px';
		bt.style.verticalAlign = 'middle';
	},
	mouseButtonLeft: function() {
		if (IsIE()) {
			return 1;
		}
		return 0;
	},
	mouseButtonRight: function() {
		return 2;
	},
	getDirectChildElementsByTagName: function(e, tag) {
		if (tag) {
			var a = e.childNodes;
			var aRet = new Array();
			if (a && a.length > 0) {
				for (var i = 0; i < a.length; i++) {
					if (a[i].parentNode == e) {
						aRet.push(a[i]);
					}
				}
			}
			return aRet;
		}
		else {
			return e.childNodes;
		}
	},
	setVisible: function(e, v) {
		var b = false;
		if (v === 'none') {
			//e.style.display = v;
			b = false;
		}
		else if (v === 'block' || v === '') {
			//e.style.display = v;
			b = true;
		}
		else {
			if (JsonDataBinding.isValueTrue(v)) {
				//e.style.display = 'block';
				v = 'block';
				b = true;
			}
			else {
				//e.style.display = 'none';
				v = 'none';
				b = false;
			}
		}
		if (e.getAttribute('usedpid')) {
			var dpdiv = document.getElementById('fd-' + e.id);
			if (dpdiv) {
				dpdiv.style.display = v;
			}
		}
		else if (e.getAttribute('useDP')) {
			e.style.display = v;
			var dpbut = document.getElementById('fd-but-' + e.id);
			if (dpbut) {
				dpbut.style.display = v;
				JsonDataBinding.adjustDatePickerButtonPos(e.id);
			}
		}
		else {
			e.style.display = v;
			if (b) {
				function adjustDPpos(c) {
					if (c.tagName.toLowerCase() == 'input') {
						JsonDataBinding.adjustDatePickerButtonPos(c.id);
					}
					else if (c.tagName.toLowerCase() == 'table') {
						if (c.jsData && c.jsData.adjustHeaderWidthes) {
							c.jsData.adjustHeaderWidthes();
						}
					}
					else {
						for (var i = 0; i < c.children.length; i++) {
							adjustDPpos(c.children[i]);
						}
					}
				}
				adjustDPpos(e);
			}
		}
	},
	colorObj: function(c) {
		var _red = 0;
		var _green = 0;
		var _blue = 0;
		function setHex(h) {
			if (h.charAt(0) == "#") h = h.substring(1);
			_red = parseInt(h.substring(0, 2), 16);
			_green = parseInt(h.substring(2, 4), 16);
			_blue = parseInt(h.substring(4, 6), 16);
		};
		function setRGB(rgb) {
			rgb = rgb.substring(4, rgb.length - 1);
			var colors = rgb.split(',');
			_red = parseInt(colors[0]);
			_green = parseInt(colors[1]);
			_blue = parseInt(colors[2]);
		}
		if (c.indexOf("rgb(") == 0)
			setRGB(c);
		else
			setHex(c);
		return {
			r: _red,
			g: _green,
			b: _blue
		};
	},
	compareColor: function(c1, c2) {
		var a = JsonDataBinding.colorObj(c1);
		var b = JsonDataBinding.colorObj(c2);
		return (a.r == b.r && a.g == b.g && a.b == b.b);
	},
	hasClass: function(e, c) {
		if (e && e.className && c) {
			var cs = e.className.split(' ');
			var c0 = c.toLowerCase();
			for (var i = 0; i < cs.length; i++) {
				if (cs[i] && cs[i].toLowerCase() == c0) {
					return true;
				}
			}
		}
	},
	//===end of generic utilities===
	//limnorDynaStyleTitle: 'dyStyle8831932',
	initMenubar: function(nav) {
		for (var m = 0; m < nav.children.length; m++) {
			var ul = nav.children[m];
			if (ul && ul.tagName && ul.tagName.toLowerCase() == 'ul') {
				for (var n = 0; n < ul.children.length; n++) {
					var li = ul.children[n];
					if (li && li.tagName && li.tagName.toLowerCase() == 'li') {
						for (var t = 0; t < li.children.length; t++) {
							var ul2 = li.children[t];
							if (ul2 && ul2.tagName && ul2.tagName.toLowerCase() == 'ul') {
								var anchs = ul2.getElementsByTagName('a');
								if (anchs) {
									for (var i = 0; i < anchs.length; i++) {
										var pli = anchs[i].parentNode;
										var isParent = false;
										if (pli && pli.tagName && pli.tagName.toLowerCase() == 'li') {
											for (var k = 0; k < pli.children.length; k++) {
												if (pli.children[k].tagName.toLowerCase() == 'ul') {
													isParent = true;
													break;
												}
											}
										}
										if (isParent) {
											anchs[i].setAttribute('isparent', '1');
										}
										else {
											anchs[i].removeAttribute('isparent');
										}
									}
								}
							}
						}
					}
				}
			}
		}
	},
	//===begin css treeview=============
	limnorTreeViewStylesName: function() { return 'limnortv'; },
	createTreeView: function(tvul) {
		if (typeof tvul.jsData != 'undefined')
			return tvul.jsData;
		tvul.typename = 'treeview';
		function _getChildUL(li) {
			if (li.children) {
				for (var i = 0; i < li.children.length; i++) {
					if (li.children[i] && li.children[i].tagName && li.children[i].tagName.toLowerCase() == 'ul') {
						return li.children[i];
					}
				}
			}
		}
		function _setDefaultStateLI(li) {
			var hasSub = false;
			var isLast = true;
			var isRoot = false;
			for (var i = 0; i < li.children.length; i++) {
				if (li.children[i].tagName.toLowerCase() == 'ul') {
					for (var k = 0; k < li.children[i].children.length; k++) {
						if (li.children[i].children[k].tagName.toLowerCase() == 'li') {
							hasSub = true;
							break;
						}
					}
					break;
				}
			}
			if (li.nextElementSibling) {
				isLast = false;
			}
			if (JsonDataBinding.hasClass(li.parentNode, 'limnortv')) {
				isRoot = true;
			}
			var st0 = li.getAttribute('expState');
			var st = 9;
			if (isRoot) {
				if (isLast) {
					var isSingle = true;
					var ulParent = li.parentNode;
					for (var i = 0; i < ulParent.children.length; i++) {
						if (ulParent.children[i] != li) {
							if (ulParent.children[i].tagName.toLowerCase() == 'li') {
								isSingle = false;
								break;
							}
						}
					}
					if (hasSub) {
						if (isSingle) {
							if (st0 != 3) {
								st = 2;
							}
							else
								st = st0;
						}
						else {
							if (st0 != 8) {
								st = 7;
							}
							else
								st = st0;
						}
					}
					else {
						if (isSingle)
							st = 0;
						else
							st = 6;
					}
				}
				else {
					var isFirst = true;
					var ulParent = li.parentNode;
					for (var i = 0; i < ulParent.children.length; i++) {
						if (ulParent.children[i].tagName.toLowerCase() == 'li') {
							if (ulParent.children[i] != li) {
								isFirst = false;
							}
							break;
						}
					}
					if (hasSub) {
						if (isFirst) {
							if (st0 != 5)
								st = 4;
							else
								st = st0;
						}
						else {
							if (st0 != 11)
								st = 10;
							else
								st = st0;
						}
					}
					else {
						if (isFirst)
							st = 1;
						else
							st = 101;
					}
				}
			}
			else {
				if (isLast) {
					if (hasSub) {
						if (st0 != 8)
							st = 7;
						else
							st = st0;
					}
					else {
						st = 6;
					}
				}
				else {
					if (hasSub) {
						if (st0 != 11)
							st = 10;
						else
							st = st0;
					}
					else {
					}
				}
			}
			if (st == 9) {
				if (typeof st0 != 'undefined') {
					li.removeAttribute('expState');
				}
			}
			else if (st != st0) {
				li.setAttribute('expState', st);
			}
			var cu = _getChildUL(li);
			if (cu) {
				_setDefaultStateUL(cu);
			}
		}
		function _setDefaultStateUL(u) {
			if (u.children && u.children.length > 0) {
				for (var i = 0; i < u.children.length; i++) {
					if (u.children[i] && u.children[i].tagName && u.children[i].tagName.toLowerCase() == 'li') {
						_setDefaultStateLI(u.children[i]);
					}
				}
			}
		}
		function _hasChildren(u) {
			if (u.children && u.children.length > 0) {
				for (var i = 0; i < u.children.length; i++) {
					if (u.children[i] && u.children[i].tagName && u.children[i].tagName.toLowerCase() == 'li') {
						return true;
					}
				}
			}
			return false;
		}
		function _addItem(ownerLI) {
			var cu = ownerLI ? _getChildUL(ownerLI) : tvul;
			if (!cu) {
				cu = document.createElement('ul');
				ownerLI.appendChild(cu);
			}
			var li = document.createElement('li');
			li.innerHTML = 'item';
			cu.appendChild(li);
			if (ownerLI)
				_setDefaultStateLI(ownerLI);
			else
				_setDefaultStateUL(cu);
			return li;
		}
		function _delItem(ownerLI) {
			var ul = ownerLI.parentNode;
			if (ul) {
				ul.removeChild(ownerLI);
				if (ul.className == 'limnortv') {
					_setDefaultStateUL(ul);
				}
				else {
					if (_hasChildren(ul)) {
						_setDefaultStateUL(ul);
					}
					else {
						var li = ul.parentNode;
						if (li && li.tagName && li.tagName.toLowerCase() == 'li') {
							li.removeChild(ul);
							ul = li.parentNode;
							if (ul && ul.tagName && ul.tagName.toLowerCase() == 'ul') {
								_setDefaultStateUL(ul);
							}
						}
					}
				}
			}
		}
		function _delChildren(owner) {
			if (owner && owner.tagName) {
				var tag = owner.tagName.toLowerCase();
				var ul;
				var li;
				if (tag == 'li')
					li = owner;
				else if (tag == 'ul') {
					if (owner.className == 'limnortv') {
						owner.innerHTML = '';
						return;
					}
					if (owner.parentNode && owner.parentNode.tagName && owner.parentNode.tagName.toLowerCase() == 'li') {
						li = owner.parentNode;
					}
				}
				if (li) {
					ul = _getChildUL(li);
					if (ul) {
						li.removeChild(ul);
					}
					ul = li.parentNode;
					if (ul && ul.tagName && ul.tagName.toLowerCase() == 'ul') {
						_setDefaultStateUL(ul);
					}
				}
			}
		}
		function _getEventLI(e) {
			e = e || window.event;
			if (e) {
				var isLi = false;
				var sender = JsonDataBinding.getSender(e);
				while (sender) {
					if (sender.tagName && sender.tagName.toLowerCase() == 'li') {
						isLi = true;
						break;
					}
					else {
						sender = sender.parentNode;
					}
				}
				if (isLi) {
					return sender;
				}
			}
		}
		function _treeviewOnMouseup(e) {
			var sender = _getEventLI(e);
			e = e || window.event;
			if (e && sender) {
				var x = (e.pageX ? e.pageX : e.clientX ? e.clientX : e.x);
				var y = (e.pageY ? e.pageY : e.clientY ? e.clientY : e.y);
				var pos = JsonDataBinding.ElementPosition.getElementPosition(sender);
				if (JsonDataBinding.IsIE()) {
					var x0 = window.pageXOffset ? window.pageXOffset : (window.scrollX ? window.scrollX : document.body.scrollLeft);
					var y0 = window.pageYOffset ? window.pageYOffset : (window.scrollY ? window.scrollY : document.body.scrollTop);
					x += x0;
					y += y0;
				}
				var c = (x > pos.x - 20 && x < pos.x + 16) && (y > pos.y && y < pos.y + 22);
				if (c) {
					var state = sender.getAttribute('expState');
					var st = state;
					if (state == 2)
						st = 3;
					else if (state == 3)
						st = 2;
					else if (state == 4)
						st = 5;
					else if (state == 5)
						st = 4;
					else if (state == 7)
						st = 8;
					else if (state == 8)
						st = 7;
					else if (state == 11)
						st = 10;
					else if (state == 10)
						st = 11;
					if (st != state) {
						sender.setAttribute('expState', st);
						if (JsonDataBinding.IsIE()) {
							sender.className = sender.className;
						}
					}
				}
				//else {
				//	if (!_disableAction) {
				//		var act = sender.getAttribute('targetURL');
				//		if (act) {
				//			var target = sender.getAttribute('target');
				//			if (JsonDataBinding.startsWithI(act, 'javascript:')) {
				//				act = act.substr(11);
				//				eval(act);
				//			}
				//			else {
				//			}
				//		}
				//	}
				//}
				return false;
			}
		}
		var _mouseoverOwner;
		var _expOwner;
		var _disableHover;
		//var _disableAction;
		function _onmouseover(e) {
			if (!_disableHover) {
				var sender = _getEventLI(e);
				if (sender) {
					if (_mouseoverOwner) {
						_mouseoverOwner.removeAttribute('hoverstate');
					}
					_mouseoverOwner = sender;
					_mouseoverOwner.setAttribute('hoverstate', '1');
				}
			}
		}
		function _onmouseout(e) {
			var sender = _getEventLI(e);
			if (sender) {
				sender.removeAttribute('hoverstate');
			}
		}
		function _onmousemove(e) {
			e = e || window.event;
			if (e) {
				var sender = _getEventLI(e);
				if (sender) {
					var ext = sender.getAttribute('expState');
					if (ext && ext != '9' && ext != '6') {
						var x = (e.pageX ? e.pageX : e.clientX ? e.clientX : e.x);
						var y = (e.pageY ? e.pageY : e.clientY ? e.clientY : e.y);
						var pos = JsonDataBinding.ElementPosition.getElementPosition(sender);
						if (JsonDataBinding.IsIE()) {
							var x0 = window.pageXOffset ? window.pageXOffset : (window.scrollX ? window.scrollX : document.body.scrollLeft);
							var y0 = window.pageYOffset ? window.pageYOffset : (window.scrollY ? window.scrollY : document.body.scrollTop);
							x += x0;
							y += y0;
						}
						var c = (x > pos.x - 20 && x < pos.x + 16) && (y > pos.y && y < pos.y + 22);
						if (c) {
							if (_expOwner != sender) {
								_expOwner = sender;
								_expOwner.setAttribute('cursorstate', '1');
							}
						}
						else {
							if (_expOwner) {
								_expOwner.removeAttribute('cursorstate');
								_expOwner = null;
							}
						}
					}
				}
				else {
					if (_expOwner) {
						_expOwner.removeAttribute('cursorstate');
						_expOwner = null;
					}
				}
			}
		}
		_setDefaultStateUL(tvul);
		JsonDataBinding.AttachEvent(tvul, 'onmouseup', _treeviewOnMouseup);
		JsonDataBinding.AttachEvent(tvul, 'onmouseover', _onmouseover);
		JsonDataBinding.AttachEvent(tvul, 'onmouseout', _onmouseout);
		JsonDataBinding.AttachEvent(tvul, 'onmousemove', _onmousemove);
		tvul.jsData = {
			typename: 'treeview',
			//createSubEditor: function (o, e) {
			//	if(o==e)
			//		return o;
			//	if (e && e.tagName && e.tagName.toLowerCase()=='li') {

			//	}
			//},
			resetState: function() {
				_setDefaultStateUL(tvul);
			},
			enableHoverState: function(enable) {
				_disableHover = !enable;
				if (_disableHover) {
					var lis = tvul.getElementsByTagName('li');
					if (lis) {
						for (var i = 0; i < lis.length; i++) {
							lis[i].removeAttribute('hoverstate');
						}
					}
				}
			},
			//enableActions: function (enable) {
			//	_disableAction = !enable;
			//},
			editable: function(e) {
				if (e) {
					if (e != tvul) {
						if (e.tagName && e.tagName.toLowerCase() == 'ul') {
							return false;
						}
					}
					return true;
				}
				return false;
			},
			addItem: function(ownerLI) {
				return _addItem(ownerLI);
			},
			delItem: function(ownerLI) {
				_delItem(ownerLI);
			},
			delChildren: function(owner) {
				_delChildren(owner);
			}
		};
		return tvul.jsData;
	},
	//===end of tree view===
	//===begin multi-selection list box===
	//multiSelBox is a div
	createMultiSelection: function(multiSelBox) {
		var _selected = [];
		function _onCancel() {
			multiSelBox.style.display = 'none';
			if (multiSelBox.onClickCancel) {
				multiSelBox.onClickCancel();
			}
		}
		function _onOK(e) {
			multiSelBox.style.display = 'none';
			if (multiSelBox.onClickOK) {
				multiSelBox.onClickOK();
			}
			if (multiSelBox.onSelectedItem) {
				var sel = multiSelBox.getElementsByTagName('select')[0];
				for (var i = 0; i < sel.options.length; i++) {
					if (sel.options[i].selected) {
						multiSelBox.onSelectedItem(e, i, sel.options[i].text, sel.options[i].value);
					}
				}
			}
		}
		function _onchange() {
			var sels = multiSelBox.getElementsByTagName('select')[0];
			var lst = sels.getElementsByTagName('option');
			if (lst && lst.length > 0) {
				for (var i = 0; i < lst.length; i++) {
					if (lst[i].selected) {
						_selected[i] = !_selected[i];
					}
					lst[i].selected = _selected[i];
				}
			}
		}
		function _init(firstTime) {
			var sels = multiSelBox.getElementsByTagName('select')[0];
			var lst = sels.getElementsByTagName('option');
			_selected = [];
			if (lst && lst.length > 0) {
				for (var i = 0; i < lst.length; i++) {
					_selected.push(lst[i].selected);
				}
			}
			if (firstTime) {
				JsonDataBinding.AttachEvent(sels, 'onchange', _onchange);
			}
		}
		function _selectAll(select) {
			var sels = multiSelBox.getElementsByTagName('select')[0];
			var lst = sels.getElementsByTagName('option');
			_selected = [];
			if (lst && lst.length > 0) {
				for (var i = 0; i < lst.length; i++) {
					lst[i].selected = select;
					_selected.push(lst[i].selected);
				}
			}
		}
		_init(true);

		multiSelBox.jsData = {
			onCancel: function() {
				_onCancel();
			},
			onOK: function() {
				_onOK();
			},
			onChildListBoxFilled: function() {
				_init(false);
			},
			selectAll: function() {
				_selectAll(true);
			},
			selectNone: function() {
				_selectAll(false);
			}
		};
		return multiSelBox.jsData;
	},
	//===end of multi-selection listbox=============
	//===virtual page styles========================
	//===end of virtual page styles=================
	initializePage: function (forEditor) {
		if (!JsonDataBinding.initializing) {
			JsonDataBinding.initializing = true;
			if (forEditor) {
				JsonDataBinding.inediting = true;
			}
			var i, objs, tag;
			objs = document.getElementsByTagName('nav');
			if (objs) {
				tag = 'limnorstyles_menu';
				for (i = 0; i < objs.length; i++) {
					if (objs[i].className) {
						if (JsonDataBinding.startsWith(objs[i].className, tag)) {
							JsonDataBinding.initMenubar(objs[i]);
						}
					}
				}
			}
			objs = document.getElementsByTagName('ul');
			if (objs) {
				tag = JsonDataBinding.limnorTreeViewStylesName();
				for (i = 0; i < objs.length; i++) {
					if (objs[i].className) {
						if (JsonDataBinding.startsWith(objs[i].className, tag)) {
							JsonDataBinding.createTreeView(objs[i]);
						}
					}
				}
			}
			objs = document.getElementsByTagName('iframe');
			if (objs) {
				for (i = 0; i < objs.length; i++) {
					tag = objs[i].getAttribute('typename');
					if (tag == 'youtube') {
						objs[i].typename = tag;
					}
				}
			}
			JsonDataBinding.stylesInitialized = true;
			//JsonDataBinding.setupChildManager();
			JsonDataBinding.ProcessPageParameters.apply(window);
		}
	}
}
