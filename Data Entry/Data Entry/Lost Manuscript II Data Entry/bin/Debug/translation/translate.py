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
    #print "No words translated"
    exit()
    
#print text_to_translate
#print translate(text_to_translate, 'en')
#uni_text = unicode(text_to_translate)

#text_to_translate = text_to_translate.decode(sys.stdin.encoding or locale.getpreferredencoding(True)).encode('utf-8')

print (translate(text_to_translate,'en'))

#print text_to_translate

#print translate(text_to_translate,'en')
#print "testing"

#print type(text_to_translate)
#print text_to_translate
#print len(sys.argv)
#print translate('bienvenue','en')
#print translate("Let's switch gears. The Olympic Green Tennis Center is the tennis center that hosted the tennis preliminaries and finals of singles and doubles for men and women at the 2008 Summer Olympics.", 'zh-CN')
#print translate("我想知道更多有关网球的", "en")
#print '哈哈。'
#print '哈哈。'.decode('utf-8').encode('gb2312')
#print '今天天气不错'.decode('utf-8').encode('cp936')
