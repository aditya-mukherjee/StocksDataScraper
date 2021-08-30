import pandas as pd

csv_file = open("E:/code/azure_python_func1/ind_nifty200list.csv", "r")
code_file = open("E:/code/azure_python_func1/code.txt", "w")

for line in csv_file.readlines():
    elements=line.split(',')
    str='{'+'\"'+elements[0]+'\"'+','+'\"'+elements[1]+'\"'+','+'\"'+elements[2]+'\"'+'}'+','+'\n'
    print(str)
    code_file.write(str)