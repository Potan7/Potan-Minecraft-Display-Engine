import os
import base64
import gzip
import json
import re
from pathlib import Path


config = {}
# 파일을 읽고 설정을 딕셔너리에 저장
with open('setting.txt', 'r', encoding='utf-8') as file:
    for line in file:
        # 줄 끝의 개행 문자 제거
        line = line.strip()
        
        # ':' 기준으로 키와 값을 나누기
        if ':' in line:
            key, value = line.split(':', 1)
            config[key] = value

# 'frame.txt' 파일에서 프레임 정보 읽어오기
frame_file_text = 'frame.txt'
frame_info = {}
frame_file = None
# 'frame.txt' 파일이 없다면 생성
if not os.path.exists(frame_file_text):
    print(f"{frame_file_text} 파일이 없습니다. 파일을 생성합니다.")
    frame_file = open(frame_file_text, 'w')  # 파일 생성
else:
    with open(frame_file_text, 'r', encoding='utf-8') as file:
        for line in file:
            line = line.strip()

            if line == '':
                continue
            
            # 파일 분리해서 숫자 추출
            value = line.split(' ')
            inter = 0
            sec = 0
            f = -1
            for st in value:
                if st[0] == 'f':
                    f = int(st[1:])
                elif st[0] == 's':
                    sec = int(st[1:])
                elif st[0] == 'i':
                    inter = int(st[1:])
            frame_info[f] = (sec, inter)



# 부모와 자식의 transforms를 곱하는 함수
def apply_transforms(parent_transforms, child_transforms):
    result = [0] * 16
    for i in range(4):
        for j in range(4):
            result[i * 4 + j] = (parent_transforms[i * 4 + 0] * child_transforms[0 + j] +
                                 parent_transforms[i * 4 + 1] * child_transforms[4 + j] +
                                 parent_transforms[i * 4 + 2] * child_transforms[8 + j] +
                                 parent_transforms[i * 4 + 3] * child_transforms[12 + j])
    return result

# 트랜스폼 데이터를 포맷팅하는 함수
def format_transformation(transforms, default_interpolation_value=None):
    transforms_str = ",".join(f"{round(t, 4)}f" for t in transforms)
    if default_interpolation_value:
        return f"{{start_interpolation: 0, interpolation_duration: {default_interpolation_value}, transformation:[{transforms_str}]}}"
    else:
        return f"{{transformation:[{transforms_str}]}}"

        

# UUID 변환 함수
def convert_uuid(nbt):
    uuid_match = re.search(r'UUID:\[I;(-?\d+),(-?\d+),(-?\d+),(-?\d+)\]', nbt)
    if uuid_match:
        a, b, c, d = uuid_match.groups()
        hex_a = hex(int(a) & 0xFFFFFFFFFFFFFFFF)[2:]  # 64-bit hexadecimal
        hex_b = hex(int(b) & 0xFFFFFFFF)[2:]          # 32-bit hexadecimal
        hex_c = hex(int(c) & 0xFFFFFFFF)[2:]          # 32-bit hexadecimal
        hex_d = hex(int(d) & 0xFFFFFFFF)[2:]          # 32-bit hexadecimal
        uuid_hex = f"{hex_a}-0-{hex_b}-{hex_c}-{hex_d}"
        nbt = re.sub(r'UUID:\[I;.*?\]', uuid_hex, nbt)  # Replace UUID with formatted string
        nbt = re.sub(r'Tags:\[[a-zA-Z0-9_]*\],?', '', nbt)  # Tags 제거
    return nbt

# Tags 처리 함수
def process_tags(nbt):
    if "Tags" in nbt:
        tags = nbt.split("Tags:[")[1].split("]")[0]
        tags = tags.split(',')
        tags = [tag.strip().replace('"', '') for tag in tags]
        tags = [tag for tag in tags if not re.search(r'.*\D0$', tag)]
        tags_str = ','.join(tags)
        return tags_str
    return ""

# child의 texture 값을 추출하는 함수
def extract_texture_value(child):
    if 'tagHead' in child and 'Value' in child['tagHead']:
        return child['tagHead']['Value']
    elif 'customTexture' in child:
        return child['customTexture']
    return None

# 출력 라인을 생성하는 함수
def generate_output_line(mode, display_type, transformation, nbt, texture_value=None, temporary_player_name=None, scoreboard_name=None, scoreboard_start_value=None):
    tags_str = process_tags(nbt)

    if texture_value:
        item_structure = f'item:{{id:player_head,components:{{"minecraft:profile":{{properties:[{{name:textures,value:"{texture_value}"}}]}}}}}}'
        transformation = transformation[:-1] + f', {item_structure}}}'

    if mode == 0 and scoreboard_name is None and scoreboard_start_value is None:
        if display_type == "isItemDisplay" and tags_str:
            return f'execute if entity @s[tag={tags_str},type=item_display] run data merge entity @s {transformation}'
        
        if display_type == "isTextDisplay" and tags_str:
            return f'execute if entity @s[tag={tags_str},type=text_display] run data merge entity @s {transformation}'
        
        if display_type == "isBlockDisplay" and tags_str:
            return f'execute if entity @s[tag={tags_str},type=block_display] run data merge entity @s {transformation}'

    if mode == 0 and display_type == "isItemDisplay" and tags_str:
        return f'execute if entity @s[tag={tags_str},type=item_display] run data merge entity @s {transformation}'

    if mode == 1 and display_type == "isItemDisplay" and tags_str:
        return f'data merge entity @e[limit=1,tag={tags_str},type=item_display] {transformation}'

    if mode == 0 and display_type == "isTextDisplay" and tags_str:
        return f'execute if entity @s[tag={tags_str},type=text_display] run data merge entity @s {transformation}'
    
    if mode == 1 and display_type == "isTextDisplay" and tags_str:
        return f'data merge entity @e[limit=1,tag={tags_str},type=text_display] {transformation}'

    if mode == 0 and display_type == "isBlockDisplay" and tags_str:
        return f'execute if entity @s[tag={tags_str},type=block_display] run data merge entity @s {transformation}'
    
    if mode == 1 and display_type == "isBlockDisplay" and tags_str:
        return f'data merge entity @e[limit=1,tag={tags_str},type=block_display] {transformation}'

    if re.search(r'\b[a-zA-Z0-9]+(-[a-zA-Z0-9]+){4}\b', nbt):
        nbt = nbt.replace(display_type, "")
        nbt = nbt.strip()
        return f'data merge entity {nbt} {transformation}'

    if mode == 0:
        return f'as {display_type} {nbt} run data merge entity @s {transformation}'
    else:
        return f'data merge entity {display_type} {nbt} {transformation}'


# 부모 트랜스폼을 자식에게 적용하는 함수
def process_transforms(parent_transforms, children, mode, default_interpolation_value, temporary_player_name, scoreboard_name, scoreboard_start_value):
    results = []
    for child in children:
        nbt = child.get("nbt", "")
        nbt = convert_uuid(nbt)  # UUID 변환 및 Tags 제거
        texture_value = extract_texture_value(child)  # 텍스처 값 추출

        if child.get('isItemDisplay'):
            child_transforms = child['transforms']
            final_transforms = apply_transforms(parent_transforms, child_transforms)
            transformation = format_transformation(final_transforms, default_interpolation_value)
            result_line = generate_output_line(mode, "isItemDisplay", transformation, nbt, texture_value, temporary_player_name, scoreboard_name, scoreboard_start_value)
            if result_line:
                results.append(result_line)

        elif child.get('isBlockDisplay'):
            child_transforms = child['transforms']
            final_transforms = apply_transforms(parent_transforms, child_transforms)
            transformation = format_transformation(final_transforms, default_interpolation_value)
            result_line = generate_output_line(mode, "isBlockDisplay", transformation, nbt, texture_value, temporary_player_name, scoreboard_name, scoreboard_start_value)
            if result_line:
                results.append(result_line)

        elif child.get('isTextDisplay'):
            child_transforms = child['transforms']
            final_transforms = apply_transforms(parent_transforms, child_transforms)
            transformation = format_transformation(final_transforms, default_interpolation_value)
            result_line = generate_output_line(mode, "isTextDisplay", transformation, nbt, texture_value, temporary_player_name, scoreboard_name, scoreboard_start_value)
            if result_line:
                results.append(result_line)

        elif child.get('isCollection'):
            results.extend(process_transforms(apply_transforms(parent_transforms, child['transforms']), child.get('children', []), mode, default_interpolation_value, temporary_player_name, scoreboard_name, scoreboard_start_value))

    return results

# 최상위 데이터를 처리하는 함수
def handle_top_level_data(data, mode, default_interpolation_value, temporary_player_name, scoreboard_name, scoreboard_start_value):
    results = []
    nbt = data.get("nbt", "")
    final_transforms = data.get('transforms', [0] * 16)

    if 'children' in data:
        results.extend(process_transforms(final_transforms, data['children'], mode, default_interpolation_value, temporary_player_name, scoreboard_name, scoreboard_start_value))

    return results

# 파일 이름에서 숫자 추출하는 함수
def extract_number_from_filename(filename):
    match = re.search(r'f(\d+)', filename)
    return int(match.group(1)) if match else float('inf')

# 파일 이름에서 i와 s 값 추출하는 함수
def extract_values_from_filename(filename):
    i_value = None
    s_value = 0
    f_value = -1
    tttal = 0

    f_match = re.search(r'f(\d+)', filename)
    if f_match:
        f_value = int(f_match.group(1))
        if f_value in frame_info:
            s_value, i_value = frame_info[f_value]
            tttal = s_value
            return i_value, s_value, tttal

    i_match = re.search(r'i(\d+)', filename)
    if i_match:
        i_value = int(i_match.group(1))

    s_match = re.search(r's(\d+)', filename)
    if s_match:
        s_value = int(s_match.group(1))
        tttal = s_value
    
    if frame_file:
        frame_file.write(f"f{f_value} s{s_value} i{i_value}\n")
        frame_info[f_value] = (s_value, i_value)
    return i_value, s_value, tttal


head_value_pattern = re.compile(r', item:\{id:player_head,components:\{"minecraft:profile":\{properties:\[\{name:textures,value:"[a-zA-Z0-9=]+"\}\]\}\}\}')
tag_pattern = re.compile(r'(tag=[a-zA-Z0-9_]+,*)+')
uuid_pattern = re.compile(r'[a-z0-9]*-[a-z0-9]*-[a-z0-9]*-[a-z0-9]*-[a-z0-9]*')
transformation_pattern = re.compile(r'transformation:\[([+-]?\d*(?:\.\d+)?f?,)*[+-]?\d*(?:\.\d+)?f?\](, )?')


# .bdengine 파일을 처리하는 메인 함수 (수정된 부분 포함)
def process_bdengine_file():
    # f숫자 형식에 맞는 .bdengine 파일만 선택
    bdengine_files = [f for f in os.listdir() if re.search(r'f\d+.*\.bdengine$', f)]
    
    # 가장 큰 숫자를 가진 파일을 먼저 처리하고, 나머지는 오름차순으로 정렬
    bdengine_files.sort(key=lambda x: extract_number_from_filename(x))  # 숫자 기준으로 정렬
    largest_file = bdengine_files[-1]  # 가장 큰 숫자를 가진 파일을 찾음
    bdengine_files.remove(largest_file)  # 그 파일을 리스트에서 제거
    bdengine_files.insert(0, largest_file)  # 그 파일을 첫 번째로 처리하도록 리스트의 맨 앞에 추가
    bdengine_files.append(largest_file)  # 그 파일을 마지막에도 추가
    # 설정 파일에서 설정값 읽어오기
    def load_config(file_path):
        config = {}
        try:
            with open(file_path, 'r', encoding='utf-8') as file:
                # 설정 1~5까지 읽어서 딕셔너리에 저장
                for line in file:
                    line = line.strip()  # 줄 끝의 개행 문자 제거
                    if ':' in line:
                        key, value = line.split(':', 1)
                        config[key.strip()] = value.strip()  # 키와 값의 앞뒤 공백 제거
        except FileNotFoundError:
            print(f"{file_path} 파일을 찾을 수 없습니다.")
            return None  # 파일을 찾을 수 없으면 None 반환
        return config
    
    
    # 설정 값 불러오기
    config = load_config('setting.txt')  # setting.txt 파일에서 설정값을 읽어옴
    
    # 설정1
    if config:
        scoreboard_start_value = 0  # 기본값을 0으로 설정
        mode_input = config.get('생성모드', None)
        mode = int(mode_input) if mode_input else 0  # 문자열을 정수형으로 변환, 값이 없으면 0
        temporary_player_name = config.get('임시플레이어(선택)', None)  # 설정3의 값을 temporary_player_name에 할당
        scoreboard_name = config.get('스코어 이름(선택)', None)  # 설정4의 값을 scoreboard_name에 할당
        default_interpolation_value_input = config.get('기본 보간값(선택)', None)
        default_s_value = config.get('기본 스코어증가값(기본값 1)', 1)
        scoreboard_start_value_input = config.get('시작 스코어 값(선택)', 0)
        if scoreboard_start_value_input.isdigit():
            scoreboard_start_value = int(scoreboard_start_value_input)
            current_score = scoreboard_start_value
        namespace = config.get('네임스페이스', None)
        frame_file_savename = config.get('score저장이름(기본값frame)', None)
        save_dnlcl = config.get('frame저장위치(선택)', None)
        save_dnlcl_ifsocre = config.get('score저장위치(선택)', None)


        # 출력
        print(f"설정 값 불러오기 성공: {config}")
    else:
        print("설정 값을 불러오는 데 실패했습니다.")
    
    # 'result' 폴더가 없다면 생성
    result_folder = 'result'
    if not os.path.exists(result_folder):
        os.makedirs(result_folder)
    # 'result' 폴더 내의 파일 삭제
    for filename in os.listdir(result_folder):
        file_path = os.path.join(result_folder, filename)
        if os.path.isfile(file_path):
            # 삭제할 파일 이름 결정
            if not frame_file_savename or frame_file_savename.strip() == "":  # frame_file_savename이 공백이거나 None인 경우
                if filename == "frame.mcfunction" or re.match(r"^f\d+\.mcfunction$", filename):
                    os.remove(file_path)
            else:  # frame_file_savename이 있는 경우
                if filename == f"{frame_file_savename}.mcfunction" or re.match(r"^f\d+\.mcfunction$", filename):
                    os.remove(file_path)
    
    # 폴더 내 파일 삭제 (save_dnlcl)
    if save_dnlcl and save_dnlcl.strip() and os.path.isdir(save_dnlcl):
        for filename in os.listdir(save_dnlcl):
            file_path = os.path.join(save_dnlcl, filename)
            if os.path.isfile(file_path):
                # 삭제할 파일 이름 결정
                if not frame_file_savename or frame_file_savename.strip() == "":
                    if filename == "frame.mcfunction" or re.match(r"^f\d+\.mcfunction$", filename):
                        os.remove(file_path)
                else:
                    if filename == f"{frame_file_savename}.mcfunction" or re.match(r"^f\d+\.mcfunction$", filename):
                        os.remove(file_path)
    
    # 폴더 내 파일 삭제 (save_dnlcl_ifsocre)
    if save_dnlcl_ifsocre and save_dnlcl_ifsocre.strip() and os.path.isdir(save_dnlcl_ifsocre):
        for filename in os.listdir(save_dnlcl_ifsocre):
            file_path = os.path.join(save_dnlcl_ifsocre, filename)
            if os.path.isfile(file_path):
                # 삭제할 파일 이름 결정
                if not frame_file_savename or frame_file_savename.strip() == "":
                    if filename == "frame.mcfunction" or re.match(r"^f\d+\.mcfunction$", filename):
                        os.remove(file_path)
                else:
                    if filename == f"{frame_file_savename}.mcfunction" or re.match(r"^f\d+\.mcfunction$", filename):
                        os.remove(file_path)
    


    # 초기 스코어를 scoreboard_start_value로 설정
    
    # Initialize first_file variable
    first_file = True  # Initialize before use
    current_head_values = {}


    # 첫 번째 파일에 대한 처리
    for bdengine_file in bdengine_files:
        i_value, s_value, tttal = extract_values_from_filename(bdengine_file)

        # i 값이 있으면 기본 보간 값 대체
        if i_value is not None:
            default_interpolation_value = f"{i_value}"  # i값으로 대체
        else:
            default_interpolation_value = default_interpolation_value_input

        # 스코어 증가, 첫 번째 파일이면 1 증가하지 않음
        if not first_file:
            
            if int(default_s_value):  # 문자열이 빈 값이 아니거나 공백이 아닌 경우
                current_score = scoreboard_start_value - int(default_s_value)
            else:
                current_score = scoreboard_start_value - 1  # 빈 문자열일 경우 기본값 사용
 



        # 현재 파일에 대해 바로 적용할 시작값 설정
        current_start_value = current_score if scoreboard_name else None

        with open(bdengine_file, 'rb') as f:
            decoded_data = base64.b64decode(f.read())
        decompressed_data = gzip.decompress(decoded_data)
        text_data = decompressed_data.decode('utf-8')

        try:
            data = json.loads(text_data)
        except json.JSONDecodeError:
            continue

        results = []

        for item in data:
            temp = handle_top_level_data(item, mode, default_interpolation_value, temporary_player_name, scoreboard_name, current_start_value)
            results.extend(temp)


        current_tags = set()
        current_transformations = {}

        # 결과 필터링
        filtered_lines = []
        for line in results:
            # 'execute if score None None run' 부분만 제거
            line = line.replace("execute if score   matches None run", "")
        
            tag_matches = tag_pattern.findall(line)
            if len(tag_matches) > 0: 
                selector_key = tag_matches[0]
            
            uuid_matches = uuid_pattern.findall(line)
            if len(uuid_matches) > 0: 
                selector_key = uuid_matches[0]
        
            # transformation_pattern.search(line)이 None이 아닌지 체크
            transformation_match = transformation_pattern.search(line)
            if transformation_match:  # 만약 match가 존재하면
                transformation_str = transformation_match[0]
            else:
                # transformation_str을 처리할 기본값 설정 또는 다른 처리
                transformation_str = ""  # 예시로 빈 문자열 처리
        
            head_value_matches = head_value_pattern.findall(line)
            
            # 이후 코드에서 transformation_str을 사용하는 부분
            # 예를 들어 필터링을 위한 추가 조건을 넣는 코드 작성
        
            
            # 현재 Tag와 transformation 추가
            current_tags.add(selector_key)
            current_transformations[selector_key] = transformation_str

            # 중복되는지 확인
            is_head = True
            if len(head_value_matches) == 0 or (not first_file and selector_key in previous_tags and
                    head_value_matches[0] == previous_head_values.get(selector_key)):
                # 중복이니 head_value 지우기
                is_head = False
                if len(head_value_matches) != 0: line = line.replace(head_value_matches[0], "")
                

            is_transformation = True
            if (not first_file and selector_key in previous_tags and
                    transformation_str == previous_transformations.get(selector_key)):
                # 중복이니 transformation 지우기
                is_transformation = False
                line = line.replace(transformation_str, "")
                line = line.replace(transformation_str, "")
                line = re.sub(r"start_interpolation: \d+, interpolation_duration: \d+", "", line)
                line = line.replace("data merge entity", "item replace entity")
                line = re.sub(r"@e\[(.*?)\]", lambda m: f"@e[{m.group(1)}] container.0 with player_head[profile={{properties:[{{name:\"textures\",value:\"", line)
                line = re.sub(r'{, item:{id:player_head,components:{"minecraft:profile":{properties:\[{name:textures,value:"', '', line)
                line = re.sub(r'\}]}}}}', '}]}]', line)
                line = re.sub(r'item replace entity @s ', 'item replace entity @s container.0 with player_head[profile={properties:[{name:\"textures\",value:\"', line)
                line = re.sub(r'item replace entity ([a-z0-9]*-[a-z0-9]*-[a-z0-9]*-[a-z0-9]*-[a-z0-9]*)', 
              r'item replace entity \g<1> container.0 with player_head[profile={properties:[{name:"textures",value:"', 
              line)
                line = re.sub(r'value:" ', 'value:"', line)


    

            # 머리, trnas 둘 다 안 들어가면 추가 하지 않기
            if not is_head and not is_transformation: continue

            if is_head and len(head_value_matches) != 0: current_head_values[selector_key] = head_value_matches[0]
            if is_transformation: current_transformations[selector_key] = transformation_str

            filtered_lines.append(line.strip())  # 공백 제거 후 추가
        filtered_results = [line for line in filtered_lines if line]  # 빈 줄은 제거


        previous_tags = current_tags
        previous_transformations = current_transformations
        previous_head_values = current_head_values

        if first_file:
            first_file = False

        # 파일 이름에서 숫자 추출
        extracted_number = extract_number_from_filename(bdengine_file)

        # 저장 경로 결정
        if save_dnlcl == "":
            # save_dnlcl이 None이면 result_folder 경로 사용
            txt_file_path = os.path.join(result_folder, f"f{extracted_number}.mcfunction")
        else:
            # save_dnlcl이 None이 아니면 save_dnlcl 경로 사용
            txt_file_path = os.path.join(save_dnlcl, f"f{extracted_number}.mcfunction")
        
        # .mcfunction 파일로 결과 저장
        with open(txt_file_path, 'w', encoding='utf-8') as txt_file:
            for line in filtered_results:
                txt_file.write(line + '\n')


    # 두 번째 파일부터 마지막 파일까지 처리
    frame_num = 0
    score_interpolation = {}


    for bdengine_file in bdengine_files[1:]:
        frame_num += 1
        i_value, s_value, tttal = extract_values_from_filename(bdengine_file)

        # i 값이 있으면 기본 보간 값 대체
        if i_value is not None:
            default_interpolation_value = f"{i_value}"  # i값으로 대체
        else:
            default_interpolation_value = default_interpolation_value_input

        default_s_value = 1 if default_s_value == '' else int(default_s_value)
        current_score += s_value if s_value else int(default_s_value)  # s가 없으면 1 증가

        if s_value == 0:
            score_interpolation[extracted_number] = current_score - tttal - int(default_s_value)
        else:
            score_interpolation[extracted_number] = current_score - tttal

        

        # 현재 파일에 대해 바로 적용할 시작값 설정
        current_start_value = current_score if scoreboard_name else None

        with open(bdengine_file, 'rb') as f:
            decoded_data = base64.b64decode(f.read())
        decompressed_data = gzip.decompress(decoded_data)
        text_data = decompressed_data.decode('utf-8')

        try:
            data = json.loads(text_data)
        except json.JSONDecodeError:
            continue

        results = []

        for item in data:
            temp = handle_top_level_data(item, mode, default_interpolation_value, temporary_player_name, scoreboard_name, current_start_value)
            results.extend(temp)

        current_tags = set()
        current_transformations = {}

        # 결과 필터링
        filtered_lines = []
        for line in results:
            # 'execute if score None None run' 부분만 제거
            line = line.replace("execute if score   matches None run", "")
            line = line.replace("if score   matches None ", "")

            tag_matches = tag_pattern.findall(line)
            if len(tag_matches) > 0 : selector_key = tag_matches[0]
            uuid_matches = uuid_pattern.findall(line)
            if len(uuid_matches) > 0 : selector_key = uuid_matches[0]
        # 결과 필터링
        filtered_lines = []
        for line in results:
            # 'execute if score None None run' 부분만 제거
            line = line.replace("execute if score   matches None run", "")
        
            tag_matches = tag_pattern.findall(line)
            if len(tag_matches) > 0: 
                selector_key = tag_matches[0]
            
            uuid_matches = uuid_pattern.findall(line)
            if len(uuid_matches) > 0: 
                selector_key = uuid_matches[0]
        
            # transformation_pattern.search(line)이 None이 아닌지 체크
            transformation_match = transformation_pattern.search(line)
            if transformation_match:  # 만약 match가 존재하면
                transformation_str = transformation_match[0]
            else:
                # transformation_str을 처리할 기본값 설정 또는 다른 처리
                transformation_str = ""  # 예시로 빈 문자열 처리
        
            head_value_matches = head_value_pattern.findall(line)
            
            # 이후 코드에서 transformation_str을 사용하는 부분
            # 예를 들어 필터링을 위한 추가 조건을 넣는 코드 작성
        
            
            # 현재 Tag와 transformation 추가
            current_tags.add(selector_key)
            current_transformations[selector_key] = transformation_str

            # 중복되는지 확인
            is_head = True
            if len(head_value_matches) == 0 or (not first_file and selector_key in previous_tags and
                    head_value_matches[0] == previous_head_values.get(selector_key)):
                # 중복이니 head_value 지우기
                is_head = False
                if len(head_value_matches) != 0: line = line.replace(head_value_matches[0], "") 


            # 중복되는지 확인
            is_transformation = True
            if (not first_file and selector_key in previous_tags and
                    transformation_str == previous_transformations.get(selector_key)):
                # 중복이니 transformation 지우기
                is_transformation = False
                line = line.replace(transformation_str, "")
                line = re.sub(r"start_interpolation: \d+, interpolation_duration: \d+", "", line)
                line = line.replace("data merge entity", "item replace entity")
                line = re.sub(r"@e\[(.*?)\]", lambda m: f"@e[{m.group(1)}] container.0 with player_head[profile={{properties:[{{name:\"textures\",value:\"", line)
                line = re.sub(r'{, item:{id:player_head,components:{"minecraft:profile":{properties:\[{name:textures,value:"', '', line)
                line = re.sub(r'\}]}}}}', '}]}]', line)
                line = re.sub(r'item replace entity @s ', 'item replace entity @s container.0 with player_head[profile={properties:[{name:\"textures\",value:\"', line)
                line = re.sub(r'item replace entity ([a-z0-9]*-[a-z0-9]*-[a-z0-9]*-[a-z0-9]*-[a-z0-9]*)', 
              r'item replace entity \g<1> container.0 with player_head[profile={properties:[{name:"textures",value:"', 
              line)
                line = re.sub(r'value:" ', 'value:"', line)
            # 머리, trans 둘 다 안 들어가면 추가 하지 않기
            if not is_head and not is_transformation: continue

            if is_head and len(head_value_matches) != 0: current_head_values[selector_key] = head_value_matches[0]
            if is_transformation: current_transformations[selector_key] = transformation_str

            filtered_lines.append(line.strip())  # 공백 제거 후 추가
        filtered_results = [line for line in filtered_lines if line]  # 빈 줄은 제거

        previous_tags = current_tags
        previous_transformations = current_transformations
        previous_head_values = current_head_values
        score_interpolation_list = []
    
        # 파일 이름에서 숫자 추출
        extracted_number = extract_number_from_filename(bdengine_file)

        # 저장 경로 결정
        if save_dnlcl == "":
            # save_dnlcl이 None이면 result_folder 경로 사용
            txt_file_path = os.path.join(result_folder, f"f{extracted_number}.mcfunction")
        else:
            # save_dnlcl이 None이 아니면 save_dnlcl 경로 사용
            txt_file_path = os.path.join(save_dnlcl, f"f{extracted_number}.mcfunction")
        
        # .mcfunction 파일로 결과 저장
        with open(txt_file_path, 'w', encoding='utf-8') as txt_file:
            for line in filtered_results:
                txt_file.write(line + '\n')

                

    if not score_interpolation_list:
    # 딕셔너리의 키-값 쌍을 튜플로 만들어 리스트로 변환
        score_interpolation_list = list(score_interpolation.items())
        score_interpolation_list.sort(key=lambda x: x[0])
    # a값을 기준으로 오름차순 정렬
 
        #끼에엑 여기만 좀 건들이면 해결
        if s_value == 0:
            score_interpolation[extracted_number] = score_interpolation_list[-2][1] + int(default_s_value)
        else:
            score_interpolation[extracted_number] = score_interpolation_list[-2][1] + s_value


    # 저장 경로 결정
    if not save_dnlcl_ifsocre:  # save_dnlcl_ifsocre가 None이거나 빈 문자열이면
        # save_dnlcl_ifsocre가 None이거나 빈 문자열이면 result_folder 경로 사용
        if not frame_file_savename or frame_file_savename.strip() == "":  # frame_file_savename이 None이나 빈 문자열인 경우
            file_path = os.path.join(result_folder, "frame.mcfunction")
        else:  # frame_file_savename이 존재하는 경우
            file_path = os.path.join(result_folder, f"{frame_file_savename}.mcfunction")
    else:
        # save_dnlcl_ifsocre가 None도 아니고 빈 문자열도 아니면 save_dnlcl_ifsocre 경로 사용
        if not frame_file_savename or frame_file_savename.strip() == "":  # frame_file_savename이 None이나 빈 문자열인 경우
            file_path = os.path.join(save_dnlcl_ifsocre, "frame.mcfunction")
        else:  # frame_file_savename이 존재하는 경우
            file_path = os.path.join(save_dnlcl_ifsocre, f"{frame_file_savename}.mcfunction")
    


    # 파일에 결과 저장
    with open(file_path, 'w', encoding='utf-8') as txt_file:  # 'w' 모드로 변경
        for key in score_interpolation:
            # 텍스트 작성
            txt_file.write(f"execute if score {temporary_player_name} {scoreboard_name} matches {score_interpolation[key]} run function {namespace}f{key}\n")
    

try:
    process_bdengine_file()
finally:
    if frame_file:
        frame_file.close()
