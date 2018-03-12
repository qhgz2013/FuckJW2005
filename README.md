# FuckJW2005
言简意赅，专门怼教务（绝望）2005的小程序

### 软件下载和使用
 [点击进入下载目录](https://github.com/qhgz2013/FuckJW2005/releases)
 
 选择最新版本下的`windows.zip`下载，运行exe即可。若运行不成功，可尝试安装.net Framework 4.0

### 说明
本软件仍在测试中。。。如有bug还请反映。。

（PS别吐槽代码风格，这个坑是从大一下开的，刚开始被c++教做人，异步和python什么的都没怎么学，重构计划无限鸽）

### 验证码识别
识别网络：简单的CNN网络

识别成功率：~99%

#### 0. 环境要求

运行环境：
- `Python` 3.6

依赖包(版本什么的下最新的就好了)：
- `numpy`
- `scikit-image`
- `tensorflow`
- `keras`
- `h5py`

搭建python的运行环境：
1. 在[Python官网](https://www.python.org/downloads/)上下载Python 3.6（记得留意选的是32-bit还是64-bit，在标题上会显示）
2. 安装python时勾上安装pip
3. 下载`numpy`依赖包：Windows版的numpy下载页面在[这里](https://www.lfd.uci.edu/~gohlke/pythonlibs/#numpy)，64bit的python选择`numpy‑x.x.x+mkl‑cp36‑cp36m‑win_amd64.whl`，32bit的python选择`numpy‑x.x.x+mkl‑cp36‑cp36m‑win32.whl`
4. 在下载后打开该文件夹，按下Shift+鼠标右键，在此处打开PowerShell，输入
```bash
pip install numpy-x.x.x+mkl-cp36-cp36m-winxxx.whl
```
右边的whl替换为下载时的文件名

5. 继续安装其他依赖包
```bash
pip install h5py scikit-image tensorflow keras
```

#### 1.1 使用预训练CNN模型
预训练的模型文件可在[这里](./ocr_project/nn_model.h5)下载，使用可见下面代码
```python
import numpy as np
import tensorflow as tf
from skimage import io
import keras
# 将one hot编码的输出转换为对应的字符串
def vec_to_str(vec):
    # 默认将预测失败的字符设置为"-"
    ret = ['-', '-', '-', '-']
    list_char = ('0','1','2','3','4','5','6','7','8','9','a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z')
    for i in range(4):
        index = int(vec[i] / 36)
        ret[index] = list_char[vec[i] % 36]
    return ret[0] + ret[1] + ret[2] + ret[3]
def main():
    # 加载模型
    nn_model = keras.models.load_model('nn_model.h5')
    # 读取图片文件，并归一化到[0,1]区间，增加的一个维度为输入的图片数量
    image = np.array([io.imread('test.gif') / 255]).astype(np.float32)
    # 预测输出
    y_pred = nn_model.predict(image)
    # 转换为字符串
    temp = y_pred[0].argsort()[-4:][::1]
    result = vec_to_str(temp)
    print(result)
if __name__ == '__main__':
    main()
```
如果你使用的是tensorflow-gpu，并且不希望tensorflow占用全部显存，可参考下面代码：
```python
import tensorflow as tf
import keras
import keras.backend.tensorflow_backend as KTF

def set_vram_growth():
    config = tf.ConfigProto()
    config.gpu_options.allow_growth = True
    sess = tf.Session(config = config)
    KTF.set_session(sess)
```
然后在main下调用`set_vram_growth()`即可

#### 1.2 自己训练CNN并使用自己的模型
1. 创建一个`training`文件夹，与`train.py`同路径（[代码戳这](./ocr_project/train.py)）
2. 将已标签的图片放到`training`文件夹中，文件名与该文件图片所显示的字符一致（即验证码为abcd的文件名为abcd.gif）
3. 输入的维度定义为72(宽)x27(高)x3(RGB通道)，格式为gif，训练集的数量越多越好
4. 运行`python train.py`，进行CNN的训练，默认迭代次数为100次
5. 训练完成后，模型的权重保存到`nn_model.h5`中

几点自己测试的结果：

训练集 1K，迭代次数 100，成功率 ~88%

训练集 2K，迭代次数 100，成功率 ~99%

### 更新
v1.6 & v1.5
- 修复issue提到的bug

v1.4
- 修复下标越界bug
- 增加对课程时间的显示

v1.3
- 完成验证码识别

v1.1 & v1.2
- 更新代码

v1.0
- 没啥的。。

### 开源协议
- GNU GPLv3
