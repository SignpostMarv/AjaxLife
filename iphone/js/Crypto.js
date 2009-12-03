var biRadixBase=2;var biRadixBits=16;var bitsPerDigit=biRadixBits;var biRadix=1<<16;var biHalfRadix=biRadix>>>1;var biRadixSquared=biRadix*biRadix;var maxDigitVal=biRadix-1;var maxInteger=9999999999999998;var maxDigits;var ZERO_ARRAY;var bigZero,bigOne;function setMaxDigits(D){maxDigits=D;ZERO_ARRAY=new Array(maxDigits);for(var C=0;C<ZERO_ARRAY.length;C++){ZERO_ARRAY[C]=0}bigZero=new BigInt();bigOne=new BigInt();bigOne.digits[0]=1}setMaxDigits(20);var dpl10=15;var lr10=biFromNumber(1000000000000000);function BigInt(B){if(typeof B=="boolean"&&B==true){this.digits=null}else{this.digits=ZERO_ARRAY.slice(0)}this.isNeg=false}function biFromDecimal(I){var J=I.charAt(0)=="-";var K=J?1:0;var G;while(K<I.length&&I.charAt(K)=="0"){++K}if(K==I.length){G=new BigInt()}else{var L=I.length-K;var H=L%dpl10;if(H==0){H=dpl10}G=biFromNumber(Number(I.substr(K,H)));K+=H;while(K<I.length){G=biAdd(biMultiply(G,lr10),biFromNumber(Number(I.substr(K,dpl10))));K+=dpl10}G.isNeg=J}return G}function biCopy(D){var C=new BigInt(true);C.digits=D.digits.slice(0);C.isNeg=D.isNeg;return C}function biFromNumber(E){var D=new BigInt();D.isNeg=E<0;E=Math.abs(E);var F=0;while(E>0){D.digits[F++]=E&maxDigitVal;E=Math.floor(E/biRadix)}return D}function reverseStr(E){var D="";for(var F=E.length-1;F>-1;--F){D+=E.charAt(F)}return D}var hexatrigesimalToChar=new Array("0","1","2","3","4","5","6","7","8","9","a","b","c","d","e","f","g","h","i","j","k","l","m","n","o","p","q","r","s","t","u","v","w","x","y","z");function biToString(I,G){var J=new BigInt();J.digits[0]=G;var H=biDivideModulo(I,J);var F=hexatrigesimalToChar[H[1].digits[0]];while(biCompare(H[0],bigZero)==1){H=biDivideModulo(H[0],J);digit=H[1].digits[0];F+=hexatrigesimalToChar[H[1].digits[0]]}return(I.isNeg?"-":"")+reverseStr(F)}function biToDecimal(G){var H=new BigInt();H.digits[0]=10;var F=biDivideModulo(G,H);var E=String(F[1].digits[0]);while(biCompare(F[0],bigZero)==1){F=biDivideModulo(F[0],H);E+=String(F[1].digits[0])}return(G.isNeg?"-":"")+reverseStr(E)}var hexToChar=new Array("0","1","2","3","4","5","6","7","8","9","a","b","c","d","e","f");function digitToHex(E){var F=15;var D="";for(i=0;i<4;++i){D+=hexToChar[E&F];E>>>=4}return reverseStr(D)}function biToHex(H){var E="";var F=biHighIndex(H);for(var G=biHighIndex(H);G>-1;--G){E+=digitToHex(H.digits[G])}return E}function charToHex(J){var O=48;var P=O+9;var N=97;var K=N+25;var L=65;var M=65+25;var I;if(J>=O&&J<=P){I=J-O}else{if(J>=L&&J<=M){I=10+J-L}else{if(J>=N&&J<=K){I=10+J-N}else{I=0}}}return I}function hexToDigit(F){var H=0;var E=Math.min(F.length,4);for(var G=0;G<E;++G){H<<=4;H|=charToHex(F.charCodeAt(G))}return H}function biFromHex(G){var J=new BigInt();var F=G.length;for(var H=F,I=0;H>0;H-=4,++I){J.digits[I]=hexToDigit(G.substr(Math.max(H-4,0),Math.min(H,4)))}return J}function biFromString(P,Q){var N=P.charAt(0)=="-";var K=N?1:0;var O=new BigInt();var M=new BigInt();M.digits[0]=1;for(var L=P.length-1;L>=K;L--){var T=P.charCodeAt(L);var S=charToHex(T);var R=biMultiplyDigit(M,S);O=biAdd(O,R);M=biMultiplyDigit(M,Q)}O.isNeg=N;return O}function biDump(B){return(B.isNeg?"-":"")+B.digits.join(" ")}function biAdd(L,H){var G;if(L.isNeg!=H.isNeg){H.isNeg=!H.isNeg;G=biSubtract(L,H);H.isNeg=!H.isNeg}else{G=new BigInt();var I=0;var J;for(var K=0;K<L.digits.length;++K){J=L.digits[K]+H.digits[K]+I;G.digits[K]=J%biRadix;I=Number(J>=biRadix)}G.isNeg=L.isNeg}return G}function biSubtract(L,H){var G;if(L.isNeg!=H.isNeg){H.isNeg=!H.isNeg;G=biAdd(L,H);H.isNeg=!H.isNeg}else{G=new BigInt();var I,J;J=0;for(var K=0;K<L.digits.length;++K){I=L.digits[K]-H.digits[K]+J;G.digits[K]=I%biRadix;if(G.digits[K]<0){G.digits[K]+=biRadix}J=0-Number(I<0)}if(J==-1){J=0;for(var K=0;K<L.digits.length;++K){I=0-G.digits[K]+J;G.digits[K]=I%biRadix;if(G.digits[K]<0){G.digits[K]+=biRadix}J=0-Number(I<0)}G.isNeg=!L.isNeg}else{G.isNeg=L.isNeg}}return G}function biHighIndex(D){var C=D.digits.length-1;while(C>0&&D.digits[C]==0){--C}return C}function biNumBits(I){var G=biHighIndex(I);var H=I.digits[G];var J=(G+1)*bitsPerDigit;var F;for(F=J;F>J-bitsPerDigit;--F){if((H&32768)!=0){break}H<<=1}return F}function biMultiply(R,S){var O=new BigInt();var T;var M=biHighIndex(R);var P=biHighIndex(S);var Q,N,L;for(var K=0;K<=P;++K){T=0;L=K;for(j=0;j<=M;++j,++L){N=O.digits[L]+R.digits[j]*S.digits[K]+T;O.digits[L]=N&maxDigitVal;T=N>>>biRadixBits}O.digits[K+M+1]=T}O.isNeg=R.isNeg!=S.isNeg;return O}function biMultiplyDigit(G,H){var I,J,K;result=new BigInt();I=biHighIndex(G);J=0;for(var L=0;L<=I;++L){K=result.digits[L]+G.digits[L]*H+J;result.digits[L]=K&maxDigitVal;J=K>>>biRadixBits}result.digits[1+I]=J;return result}function arrayCopy(M,J,O,K,L){var I=Math.min(J+L,M.length);for(var N=J,P=K;N<I;++N,++P){O[P]=M[N]}}var highBitMasks=new Array(0,32768,49152,57344,61440,63488,64512,65024,65280,65408,65472,65504,65520,65528,65532,65534,65535);function biShiftLeft(P,J){var N=Math.floor(J/bitsPerDigit);var I=new BigInt();arrayCopy(P.digits,0,I.digits,N,I.digits.length-N);var K=J%bitsPerDigit;var O=bitsPerDigit-K;for(var M=I.digits.length-1,L=M-1;M>0;--M,--L){I.digits[M]=((I.digits[M]<<K)&maxDigitVal)|((I.digits[L]&highBitMasks[K])>>>(O))}I.digits[0]=((I.digits[M]<<K)&maxDigitVal);I.isNeg=P.isNeg;return I}var lowBitMasks=new Array(0,1,3,7,15,31,63,127,255,511,1023,2047,4095,8191,16383,32767,65535);function biShiftRight(P,J){var O=Math.floor(J/bitsPerDigit);var I=new BigInt();arrayCopy(P.digits,O,I.digits,0,P.digits.length-O);var L=J%bitsPerDigit;var K=bitsPerDigit-L;for(var N=0,M=N+1;N<I.digits.length-1;++N,++M){I.digits[N]=(I.digits[N]>>>L)|((I.digits[M]&lowBitMasks[L])<<K)}I.digits[I.digits.length-1]>>>=L;I.isNeg=P.isNeg;return I}function biMultiplyByRadixPower(F,E){var D=new BigInt();arrayCopy(F.digits,0,D.digits,E,D.digits.length-E);return D}function biDivideByRadixPower(F,E){var D=new BigInt();arrayCopy(F.digits,E,D.digits,0,D.digits.length-E);return D}function biModuloByRadixPower(F,E){var D=new BigInt();arrayCopy(F.digits,0,D.digits,0,E);return D}function biCompare(D,E){if(D.isNeg!=E.isNeg){return 1-2*Number(D.isNeg)}for(var F=D.digits.length-1;F>=0;--F){if(D.digits[F]!=E.digits[F]){if(D.isNeg){return 1-2*Number(D.digits[F]>E.digits[F])}else{return 1-2*Number(D.digits[F]<E.digits[F])}}}return 0}function biDivideModulo(g,h){var n=biNumBits(g);var k=biNumBits(h);var l=h.isNeg;var b,c;if(n<k){if(g.isNeg){b=biCopy(bigOne);b.isNeg=!h.isNeg;g.isNeg=false;h.isNeg=false;c=biSubtract(h,g);g.isNeg=true;h.isNeg=l}else{b=new BigInt();c=biCopy(g)}return new Array(b,c)}b=new BigInt();c=g;var e=Math.ceil(k/bitsPerDigit)-1;var f=0;while(h.digits[e]<biHalfRadix){h=biShiftLeft(h,1);++f;++k;e=Math.ceil(k/bitsPerDigit)-1}c=biShiftLeft(c,f);n+=f;var Y=Math.ceil(n/bitsPerDigit)-1;var T=biMultiplyByRadixPower(h,Y-e);while(biCompare(c,T)!=-1){++b.digits[Y-e];c=biSubtract(c,T)}for(var V=Y;V>e;--V){var d=(V>=c.digits.length)?0:c.digits[V];var U=(V-1>=c.digits.length)?0:c.digits[V-1];var W=(V-2>=c.digits.length)?0:c.digits[V-2];var X=(e>=h.digits.length)?0:h.digits[e];var m=(e-1>=h.digits.length)?0:h.digits[e-1];if(d==X){b.digits[V-e-1]=maxDigitVal}else{b.digits[V-e-1]=Math.floor((d*biRadix+U)/X)}var Z=b.digits[V-e-1]*((X*biRadix)+m);var a=(d*biRadixSquared)+((U*biRadix)+W);while(Z>a){--b.digits[V-e-1];Z=b.digits[V-e-1]*((X*biRadix)|m);a=(d*biRadix*biRadix)+((U*biRadix)+W)}T=biMultiplyByRadixPower(h,V-e-1);c=biSubtract(c,biMultiplyDigit(T,b.digits[V-e-1]));if(c.isNeg){c=biAdd(c,T);--b.digits[V-e-1]}}c=biShiftRight(c,f);b.isNeg=g.isNeg!=l;if(g.isNeg){if(l){b=biAdd(b,bigOne)}else{b=biSubtract(b,bigOne)}h=biShiftRight(h,f);c=biSubtract(h,c)}if(c.digits[0]==0&&biHighIndex(c)==0){c.isNeg=false}return new Array(b,c)}function biDivide(C,D){return biDivideModulo(C,D)[0]}function biModulo(C,D){return biDivideModulo(C,D)[1]}function biMultiplyMod(F,E,D){return biModulo(biMultiply(F,E),D)}function biPow(H,F){var E=bigOne;var G=H;while(true){if((F&1)!=0){E=biMultiply(E,G)}F>>=1;if(F==0){break}G=biMultiply(G,G)}return E}function biPowMod(K,H,L){var G=bigOne;var J=K;var I=H;while(true){if((I.digits[0]&1)!=0){G=biMultiplyMod(G,J,L)}I=biShiftRight(I,1);if(I.digits[0]==0&&biHighIndex(I)==0){break}J=biMultiplyMod(J,J,L)}return G}
function BarrettMu(A){this.modulus=biCopy(A);this.k=biHighIndex(this.modulus)+1;var B=new BigInt();B.digits[2*this.k]=1;this.mu=biDivide(B,this.modulus);this.bkplus1=new BigInt();this.bkplus1.digits[this.k+1]=1;this.modulo=BarrettMu_modulo;this.multiplyMod=BarrettMu_multiplyMod;this.powMod=BarrettMu_powMod}function BarrettMu_modulo(H){var G=biDivideByRadixPower(H,this.k-1);var E=biMultiply(G,this.mu);var D=biDivideByRadixPower(E,this.k+1);var C=biModuloByRadixPower(H,this.k+1);var I=biMultiply(D,this.modulus);var B=biModuloByRadixPower(I,this.k+1);var A=biSubtract(C,B);if(A.isNeg){A=biAdd(A,this.bkplus1)}var F=biCompare(A,this.modulus)>=0;while(F){A=biSubtract(A,this.modulus);F=biCompare(A,this.modulus)>=0}return A}function BarrettMu_multiplyMod(A,C){var B=biMultiply(A,C);return this.modulo(B)}function BarrettMu_powMod(B,E){var A=new BigInt();A.digits[0]=1;var C=B;var D=E;while(true){if((D.digits[0]&1)!=0){A=this.multiplyMod(A,C)}D=biShiftRight(D,1);if(D.digits[0]==0&&biHighIndex(D)==0){break}C=this.multiplyMod(C,C)}return A}
function RSAKeyPair(B,C,A){this.e=biFromHex(B);this.d=biFromHex(C);this.m=biFromHex(A);this.digitSize=2*biHighIndex(this.m)+2;this.chunkSize=this.digitSize-11;this.radix=16;this.barrett=new BarrettMu(this.m)}function twoDigit(A){return(A<10?"0":"")+String(A)}function encryptedString(L,O){if(L.chunkSize>L.digitSize-11){return"Error"}var K=new Array();var A=O.length;var E=0;while(E<A){K[E]=O.charCodeAt(E);E++}var F=K.length;var P="";var D,C,B;for(E=0;E<F;E+=L.chunkSize){B=new BigInt();D=0;var J;var I=(E+L.chunkSize)>F?F%L.chunkSize:L.chunkSize;var G=new Array();for(J=0;J<I;J++){G[J]=K[E+I-1-J]}G[I]=0;var H=Math.max(8,L.digitSize-3-I);for(J=0;J<H;J++){G[I+1+J]=Math.floor(Math.random()*254)+1}G[L.digitSize-2]=2;G[L.digitSize-1]=0;for(C=0;C<L.digitSize;++D){B.digits[D]=G[C++];B.digits[D]+=G[C++]<<8}var N=L.barrett.powMod(B,L.e);var M=L.radix==16?biToHex(N):biToString(N,L.radix);P+=M+" "}return P.substring(0,P.length-1)}function decryptedString(E,F){var H=F.split(" ");var A="";var D,C,G;for(D=0;D<H.length;++D){var B;if(E.radix==16){B=biFromHex(H[D])}else{B=biFromString(H[D],E.radix)}G=E.barrett.powMod(B,E.d);for(C=0;C<=biHighIndex(G);++C){A+=String.fromCharCode(G.digits[C]&255,G.digits[C]>>8)}}if(A.charCodeAt(A.length-1)==0){A=A.substring(0,A.length-1)}return A}
function md5(M){var J=0;var H=8;function D(X,S){X[S>>5]|=128<<((S)%32);X[(((S+64)>>>9)<<4)+14]=S;var W=1732584193;var V=-271733879;var U=-1732584194;var T=271733878;for(var P=0;P<X.length;P+=16){var R=W;var Q=V;var O=U;var N=T;W=G(W,V,U,T,X[P+0],7,-680876936);T=G(T,W,V,U,X[P+1],12,-389564586);U=G(U,T,W,V,X[P+2],17,606105819);V=G(V,U,T,W,X[P+3],22,-1044525330);W=G(W,V,U,T,X[P+4],7,-176418897);T=G(T,W,V,U,X[P+5],12,1200080426);U=G(U,T,W,V,X[P+6],17,-1473231341);V=G(V,U,T,W,X[P+7],22,-45705983);W=G(W,V,U,T,X[P+8],7,1770035416);T=G(T,W,V,U,X[P+9],12,-1958414417);U=G(U,T,W,V,X[P+10],17,-42063);V=G(V,U,T,W,X[P+11],22,-1990404162);W=G(W,V,U,T,X[P+12],7,1804603682);T=G(T,W,V,U,X[P+13],12,-40341101);U=G(U,T,W,V,X[P+14],17,-1502002290);V=G(V,U,T,W,X[P+15],22,1236535329);W=C(W,V,U,T,X[P+1],5,-165796510);T=C(T,W,V,U,X[P+6],9,-1069501632);U=C(U,T,W,V,X[P+11],14,643717713);V=C(V,U,T,W,X[P+0],20,-373897302);W=C(W,V,U,T,X[P+5],5,-701558691);T=C(T,W,V,U,X[P+10],9,38016083);U=C(U,T,W,V,X[P+15],14,-660478335);V=C(V,U,T,W,X[P+4],20,-405537848);W=C(W,V,U,T,X[P+9],5,568446438);T=C(T,W,V,U,X[P+14],9,-1019803690);U=C(U,T,W,V,X[P+3],14,-187363961);V=C(V,U,T,W,X[P+8],20,1163531501);W=C(W,V,U,T,X[P+13],5,-1444681467);T=C(T,W,V,U,X[P+2],9,-51403784);U=C(U,T,W,V,X[P+7],14,1735328473);V=C(V,U,T,W,X[P+12],20,-1926607734);W=K(W,V,U,T,X[P+5],4,-378558);T=K(T,W,V,U,X[P+8],11,-2022574463);U=K(U,T,W,V,X[P+11],16,1839030562);V=K(V,U,T,W,X[P+14],23,-35309556);W=K(W,V,U,T,X[P+1],4,-1530992060);T=K(T,W,V,U,X[P+4],11,1272893353);U=K(U,T,W,V,X[P+7],16,-155497632);V=K(V,U,T,W,X[P+10],23,-1094730640);W=K(W,V,U,T,X[P+13],4,681279174);T=K(T,W,V,U,X[P+0],11,-358537222);U=K(U,T,W,V,X[P+3],16,-722521979);V=K(V,U,T,W,X[P+6],23,76029189);W=K(W,V,U,T,X[P+9],4,-640364487);T=K(T,W,V,U,X[P+12],11,-421815835);U=K(U,T,W,V,X[P+15],16,530742520);V=K(V,U,T,W,X[P+2],23,-995338651);W=E(W,V,U,T,X[P+0],6,-198630844);T=E(T,W,V,U,X[P+7],10,1126891415);U=E(U,T,W,V,X[P+14],15,-1416354905);V=E(V,U,T,W,X[P+5],21,-57434055);W=E(W,V,U,T,X[P+12],6,1700485571);T=E(T,W,V,U,X[P+3],10,-1894986606);U=E(U,T,W,V,X[P+10],15,-1051523);V=E(V,U,T,W,X[P+1],21,-2054922799);W=E(W,V,U,T,X[P+8],6,1873313359);T=E(T,W,V,U,X[P+15],10,-30611744);U=E(U,T,W,V,X[P+6],15,-1560198380);V=E(V,U,T,W,X[P+13],21,1309151649);W=E(W,V,U,T,X[P+4],6,-145523070);T=E(T,W,V,U,X[P+11],10,-1120210379);U=E(U,T,W,V,X[P+2],15,718787259);V=E(V,U,T,W,X[P+9],21,-343485551);W=F(W,R);V=F(V,Q);U=F(U,O);T=F(T,N)}return Array(W,V,U,T)}function B(S,P,O,N,R,Q){return F(I(F(F(P,S),F(N,Q)),R),O)}function G(P,O,T,S,N,R,Q){return B((O&T)|((~O)&S),P,O,N,R,Q)}function C(P,O,T,S,N,R,Q){return B((O&S)|(T&(~S)),P,O,N,R,Q)}function K(P,O,T,S,N,R,Q){return B(O^T^S,P,O,N,R,Q)}function E(P,O,T,S,N,R,Q){return B(T^(O|(~S)),P,O,N,R,Q)}function F(N,Q){var P=(N&65535)+(Q&65535);var O=(N>>16)+(Q>>16)+(P>>16);return(O<<16)|(P&65535)}function I(N,O){return(N<<O)|(N>>>(32-O))}function A(Q){var P=Array();var N=(1<<H)-1;for(var O=0;O<Q.length*H;O+=H){P[O>>5]|=(Q.charCodeAt(O/H)&N)<<(O%32)}return P}function L(P){var O=J?"0123456789ABCDEF":"0123456789abcdef";var Q="";for(var N=0;N<P.length*4;N++){Q+=O.charAt((P[N>>2]>>((N%4)*8+4))&15)+O.charAt((P[N>>2]>>((N%4)*8))&15)}return Q}return L(D(A(M),M.length*H))}
function base64encode(I){var A="ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";var G=new Array(-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,62,-1,-1,-1,63,52,53,54,55,56,57,58,59,60,61,-1,-1,-1,-1,-1,-1,-1,0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,-1,-1,-1,-1,-1,-1,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,-1,-1,-1,-1,-1);var D,F,H;var E,C,B;H=I.length;F=0;D="";while(F<H){E=I.charCodeAt(F++)&255;if(F==H){D+=A.charAt(E>>2);D+=A.charAt((E&3)<<4);D+="==";break}C=I.charCodeAt(F++);if(F==H){D+=A.charAt(E>>2);D+=A.charAt(((E&3)<<4)|((C&240)>>4));D+=A.charAt((C&15)<<2);D+="=";break}B=I.charCodeAt(F++);D+=A.charAt(E>>2);D+=A.charAt(((E&3)<<4)|((C&240)>>4));D+=A.charAt(((C&15)<<2)|((B&192)>>6));D+=A.charAt(B&63)}return D}