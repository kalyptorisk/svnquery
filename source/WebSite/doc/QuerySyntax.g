
grammar QuerySyntax;

tokens {
	LEFT 	  = '(' ;
	RIGHT 	  = ')';
	MUST	  = '+' ;
	MUST_NOT  = '-' ;
	SHOULD    = '#' ; // +asdf #asdfsdf
}
p_query	:  p_expr EOF;

p_expr	:	p_term*;

p_op	:	(MUST | MUST_NOT | SHOULD) p_term;

p_term 	:        p_atom | LEFT p_expr RIGHT; 

p_atom	:	FIELD? VALUE;

VALUE 	: 	('"'.*'"')) ;
		
FIELD 	:	'a'..'z'':';
WS      : 	(' '|'\t'|'\r'|'\n')+ { $channel = HIDDEN;} ;
