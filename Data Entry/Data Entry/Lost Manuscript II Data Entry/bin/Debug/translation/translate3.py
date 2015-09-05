# for python3

#! /usr/bin/python
# -*- coding: utf-8 -*-

import goslate
import sys

def translate(text, language):
    gs = goslate.Goslate()
    return gs.translate(text, language)

#text_to_translate = ""
if(len(sys.argv) > 1):
    text_to_translate = sys.argv[1]
else:
    print("No words translated")
    exit()
    
##text_to_translate = text_to_translate.decode(sys.stdin.encoding or locale.getpreferredencoding(True)).encode('utf-8')
#print text_to_translate
print(translate(text_to_translate,'en'))