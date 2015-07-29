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
    print "No words translated"
    exit()
    
#print text_to_translate
#print translate(text_to_translate, 'en')
#uni_text = unicode(text_to_translate)

text_to_translate = text_to_translate.decode(sys.stdin.encoding or locale.getpreferredencoding(True)).encode('utf-8')
#print text_to_translate
print translate(text_to_translate,'en')


#print type(text_to_translate)
#print text_to_translate
#print len(sys.argv)
#print translate('bienvenue', 'zh-CN')
#print translate("我想知道更多有关网球的", "en")
#print '哈哈。'
#print '哈哈。'.decode('utf-8').encode('gb2312')
#print '今天天气不错'.decode('utf-8').encode('cp936')