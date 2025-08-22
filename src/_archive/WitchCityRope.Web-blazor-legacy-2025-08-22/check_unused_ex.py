import re
import sys

def check_file(filename):
    with open(filename, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Find all catch blocks with Exception ex
    pattern = r'catch\s*\(\s*Exception\s+ex\s*\)(.*?)(?=catch\s*\( < /dev/null | finally|^\s*}|$)'
    matches = re.finditer(pattern, content, re.DOTALL | re.MULTILINE)
    
    unused_catches = []
    for match in matches:
        catch_block = match.group(1)
        # Check if 'ex' is used in the catch block
        if not re.search(r'\bex\b', catch_block):
            # Find line number
            line_num = content[:match.start()].count('\n') + 1
            unused_catches.append(line_num)
    
    return unused_catches

# Process each file
files = sys.argv[1:]
for file in files:
    unused = check_file(file)
    if unused:
        print(f"{file}: {', '.join(map(str, unused))}")
