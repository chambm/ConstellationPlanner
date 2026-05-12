import json

with open('C:/Users/Matt/Downloads/ksp-claude/ConstellationPlanner/coastline_raw.json') as f:
    data = json.load(f)

out_lines = []
for feat in data['features']:
    geom = feat['geometry']
    if geom['type'] == 'LineString':
        coords = geom['coordinates']
        line = ';'.join(f'{lon:.1f},{lat:.1f}' for lon, lat in coords)
        out_lines.append(line)
    elif geom['type'] == 'MultiLineString':
        for ls in geom['coordinates']:
            line = ';'.join(f'{lon:.1f},{lat:.1f}' for lon, lat in ls)
            out_lines.append(line)

out = '\n'.join(out_lines)
with open('C:/Users/Matt/Downloads/ksp-claude/ConstellationPlanner/ConstellationPlanner.Cli/coastline.txt', 'w') as f:
    f.write(out)
print(f'lines: {len(out_lines)}, bytes: {len(out)}')
