import numpy as np
import tensorflow as tf
import keras
from keras import backend as K
from skimage import io
import os

def load_training_set(training_set_path):
    training_list = os.listdir(training_set_path)
    temp_training_x_list = []
    temp_training_y_list = []
    for file in training_list:
        image = io.imread(training_set_path + '/' + file)
        classify_data = np.zeros((144))
        for i in range(4):
            base_offset = i * 36
            if (ord(file[i]) >= ord('0') and ord(file[i]) <= ord('9')):
                base_offset += ord(file[i]) - ord('0')
            else:
                base_offset += ord(file[i]) - ord('a') + 10
            classify_data[base_offset] = 1.
        temp_training_x_list.append(image)
        temp_training_y_list.append(classify_data)
    training_x = np.zeros((len(temp_training_x_list), 27, 72, 3))
    training_y = np.zeros((len(temp_training_y_list), 144))
    for i in range(training_x.shape[0]):
        training_x[i] = temp_training_x_list[i] / 255
        training_y[i] = temp_training_y_list[i]
    return training_x, training_y

def model(input_shape):
    X_Input = keras.layers.Input(input_shape)
    
    X = keras.layers.Conv2D(32, (5, 5), strides = (1, 1), name = 'conv0', padding = 'SAME')(X_Input)
    X = keras.layers.BatchNormalization(axis = 3, name = 'bn0')(X)
    X = keras.layers.Activation('relu')(X)
    
    X = keras.layers.MaxPooling2D((2, 2), strides = (2, 2), name = 'pool0')(X)
    
    X = keras.layers.Conv2D(64, (5, 5), strides = (1, 1), name = 'conv1', padding = 'SAME')(X)
    X = keras.layers.BatchNormalization(axis = 3, name = 'bn1')(X)
    X = keras.layers.Activation('relu')(X)
    
    X = keras.layers.MaxPooling2D((2, 2), strides = (2, 2), name = 'pool1')(X)
    
    X = keras.layers.Conv2D(64, (5, 5), strides = (1, 1), name = 'conv2', padding = 'SAME')(X)
    X = keras.layers.BatchNormalization(axis = 3, name = 'bn2')(X)
    X = keras.layers.Activation('relu')(X)
    
    X = keras.layers.MaxPooling2D((2, 2), strides = (2, 2), name = 'pool2')(X)
    
    X = keras.layers.Flatten()(X)
    X = keras.layers.Dense(144, activation = None, name = 'fc')(X)
    X = keras.layers.Activation('sigmoid')(X)
    
    model = keras.models.Model(inputs = X_Input, outputs = X, name = 'cnn')
    return model

if __name__ == '__main__':
    training_x, training_y = load_training_set('training')
    nn_model = model((27, 72, 3))
    nn_model.summary()
    nn_model.compile(optimizer = 'adam', loss = 'binary_crossentropy', metrics = ['accuracy'])
    nn_model.fit(x = training_x, y = training_y, epochs = 100, batch_size = 32)
    nn_model.save('nn_model.h5')
