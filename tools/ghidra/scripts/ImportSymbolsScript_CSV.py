#Imports a CSV file with lines in the form "0xADDRESS,symbolName"
#@category Data
#@author Roger Clark. Based on the original ImportSymbolsScript.py
 
f = askFile("Give me a file to open", "Go baby go!")

for line in file(f.absolutePath):  # note, cannot use open(), since that is in GhidraScript
  pieces = line.split(",")
  address = toAddr(long(pieces[0], 16))
  symbolName = pieces[1].rstrip() # some CSV's have a newline at the end which needs to be removed
  print "creating symbol", symbolName, "at address", address
  createSymbol(address, symbolName, False)
