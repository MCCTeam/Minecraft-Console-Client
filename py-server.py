from websocket_server import WebsocketServer
import logging
import requests
import json

# 群号
GROUP_ID = 12345678
# Onebot 发送群聊消息的节点
QQ_BOT_API = f"http://127.0.0.1:5700/send_group_msg?group_id={GROUP_ID}&message="
# 用来查询玩家列表的关键词
LIST_WORD = "#!list"
# 第一个连接websocket的一定要是onebot
onebot_id = -1
# 已关闭的连接列表
closed_list = {-1}

def new_client(client, server):
	client_id = client["id"]
	client_addr = client["address"]
	global onebot_id
	if (onebot_id == -1):
		print("Onebot client join us, you can start mcc client now.")
		onebot_id = client["id"]
	else:
		print("New mcc client join us, ")
	
	print(f"id:{client_id}, address:{client_addr}")

def send_to_qq(msg):
	requests.get(QQ_BOT_API + msg)

def on_client_closed(client, server):
	global closed_list
	closed_list.add(client["id"])

def on_get_msg(client, server, message):
	client_id = client["id"]
	print(f"client id: {client_id}, message: {message}")

	if (message.startswith(LIST_WORD)):
		send_to_qq(message[len(LIST_WORD):])
		return
	
	if (message.startswith("{")): 
		handle_qq_json(message)
		return

	# 如果不是 #!list 消息和 qq消息，那就是服里发来的消息
	# 发到其他服
	send_to_mccs(message, client, True)

	# 发到qq
	send_to_qq(message)

def handle_qq_json(msg):

	qq_json = json.loads(msg)
	if (qq_json["post_type"] != "message"):return
	if (qq_json["message_type"] != "group"):return
	if (qq_json["sub_type"] != "normal"):return
	if (qq_json["group_id"] != GROUP_ID):return
	
	sender = qq_json["sender"]["nickname"]
	result = ""
	"""
	Message example:
	"message":[
		{"data":{"text":"首先是需要加个材质包"},"type":"text"},
		{"data":{"file":"1356f3c15301f85246b5e15dda88294e","url":"http://gchat.qpic.cn/gchatpic_new/0/0-0-1356F3C15301F85246B5E15DDA88294E/0?term=2"},"type":"image"}
		]
	"""
	for item in qq_json["message"]:
		data_type = item["type"]
		if (data_type == "text"):
			result += item["data"]["text"]
			continue
		if (data_type == "face"):
			result += "[表情]"
			# TODO see https://docs-v1.zhamao.xin/face_list.html
			# face to image
			continue
		if (data_type == "image"):
			image_url = item["data"]["url"]
			result += f"[[CICode,url={image_url},name=图片]]";
			continue
		if (data_type == "mface"):
			result += "[动画表情]"

	if (result == LIST_WORD):
		# Let those MCCs return with "#!list [1.20] 在线玩家：..." which starts with #!list
		python_server.send_message_to_all(LIST_WORD)
		return
	
	# Else it is a normal message, send to all mcc
	send_to_mccs(f"<{sender}> {result}", None, True)

def send_to_mccs(msg, except_mcc_client=None, except_onebot=False):
    for client_item in python_server.clients:
        if client_item["id"] in closed_list:
            continue
        if except_mcc_client is not None and client_item["id"] == except_mcc_client["id"]:
            continue
        if except_onebot and client_item["id"] == onebot_id:
            continue
        python_server.send_message(client_item, msg)

python_server = WebsocketServer(host='127.0.0.1', port=12345, loglevel=logging.INFO)
python_server.set_fn_new_client(new_client)
python_server.set_fn_client_left(on_client_closed)
python_server.set_fn_message_received(on_get_msg)
python_server.run_forever(True)
print("输入回车以停止")
input()
python_server.shutdown_gracefully()