import numpy as np
import tensorflow as tf
import keras
from keras import backend as K
from skimage import io
import os
import requests
import base64
import re
import sys
import socket

def vec_to_str(vec):
    ret = ['-', '-', '-', '-']
    list_char = ('0','1','2','3','4','5','6','7','8','9','a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z')
    for i in range(4):
        index = int(vec[i] / 36)
        ret[index] = list_char[vec[i] % 36]
    return ret[0] + ret[1] + ret[2] + ret[3]

nn_model = keras.models.load_model('nn_model.h5')

if __name__ == '__main__':
    print("CNN Captcha Server v1.0")
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.bind(("localhost", 10086))
    server.listen(0)
    connection, address = server.accept()
    while True:

        # first 16 bytes: guid + binary data
        data = connection.recv(4096)
        if (len(data) < 16):
            continue
        guid = data[0:16]
        raw_image = data[16:]

        file = open('temp.gif', 'wb')
        file.write(raw_image)
        file.close()

        img = np.zeros((1, 27, 72, 3))
        img[0] = io.imread('temp.gif') / 255
        y_pred = nn_model.predict(img)
        temp = y_pred[0].argsort()[-4:][::-1]
        result = vec_to_str(temp)

        connection.send(guid + bytes(result, 'utf-8'))
    connection.close()