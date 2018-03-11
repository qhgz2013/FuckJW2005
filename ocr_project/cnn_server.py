import numpy as np
import tensorflow as tf
import keras
from keras import backend as K
from skimage import io
import socket
import struct
import uuid
import os


def vec_to_str(vec):
    ret = ['-', '-', '-', '-']
    list_char = ('0','1','2','3','4','5','6','7','8','9','a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z')
    for i in range(4):
        index = int(vec[i] / 36)
        ret[index] = list_char[vec[i] % 36]
    return ret[0] + ret[1] + ret[2] + ret[3]


if __name__ == '__main__':
    nn_model = keras.models.load_model('nn_model.h5')
    print("CNN Captcha Server v1.1")
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.bind(("localhost", 10086))
    server.listen(5)
    while True:
        connection, address = server.accept()
        print("Connection established:", address)
        try:
            # first 4 bytes: length
            connection.settimeout(5.0)
            data = connection.recv(4096)
            connection.settimeout(None)
            if len(data) < 4:
                print("Received less than 4 bytes data, ignored")
                continue
            length = struct.unpack(">i", data[0:4])[0]
            data = data[4:]
            while len(data) < length:
                print('Wait for continuous data:', len(data), 'of', length)
                data += connection.recv(length)
            if len(data) > length:
                print("Length incorrect, expected", length, ", but got", len(data))
                continue
            raw_image = data

            filename = str(uuid.uuid4()) + '.gif'
            file = open(filename, 'wb')
            file.write(raw_image)
            file.close()

            img = np.zeros((1, 27, 72, 3))
            img[0] = io.imread(filename) / 255
            os.remove(filename)
            y_pred = nn_model.predict(img)
            temp = y_pred[0].argsort()[-4:][::-1]
            result = vec_to_str(temp)

            connection.send(bytes(result, 'utf-8'))
            print('Result sent:', result, ', wait for ACK.')
            connection.settimeout(5.0)
            connection.recv(1024)
            connection.settimeout(None)
        except:
            print('Connection timed out')
        finally:
            print('Connection closed:', address)
            connection.close()
