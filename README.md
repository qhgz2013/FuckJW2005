# FuckJW2005
言简意赅，一个教务（绝望）2005的通选抢课小程序

### 软件下载和使用
 [点击进入下载目录](https://github.com/qhgz2013/FuckJW2005/releases)
 
 选择最新版本下的`windows.zip`下载，运行exe即可。若运行不成功，可尝试安装.net Framework 4.0。  
 若有任何使用问题，可以在Issue中反馈。
 
 下载完后，运行`FuckJW2005`，输入完学号、密码和验证码后点击登陆，在课程名称下选择你要抢的通选课，点`开干`就可以了，注意核对教师姓名和上课时间即可。
 
 本程序提供验证码的自动识别服务，不过使用流程复杂（用户体验极差），需要的可以看下面的验证码自动识别部分

### 说明
**本程序已经不再提供维护，并且仅限于编程技术交流使用，任何使用本程序的后果由使用者承担。**

### 验证码识别

#### 0. 环境要求

搭建python的运行环境：
1. 在[Python官网](https://www.python.org/downloads/)上下载Python 3.6，并安装
2. 按`Win+R`打开“运行”，填入`cmd`后输入以下指令安装其他依赖包
```bash
pip install numpy h5py scikit-image tensorflow keras
```

#### 1. 在本程序中使用验证码识别
在主界面上点击`设置python环境`，然后找到python的位置即可，一般安装在`?:\Program Files\Python36`或者`?:\Program Files (x86)\Python36`下，找到后选中该文件点确定即可。

### 无关信息，验证码的CNN模型
```text
输入：27x72x3
-> Conv2D (ksize: 5, stride: 1, padding: same, filter: 32) -> BatchNorm -> ReLU -> MaxPool2D (ksize: 2, stride: 2)
-> Conv2D (ksize: 5, stride: 1, padding: same, filter: 64) -> BatchNorm -> ReLU -> MaxPool2D (ksize: 2, stride: 2)
-> Conv2D (ksize: 5, stride: 1, padding: same, filter: 64) -> BatchNorm -> ReLU -> MaxPool2D (ksize: 2, stride: 2)
-> Flatten -> Dense(144) -> Sigmoid
```
最后得到的是144维的二分类输出，对应的是4个字符\*36（10个数字+26个字母）个二分类结果  
（大二训练的，还没学机器学习，所以没用Softmax，别吐槽就好了）

训练集见[captcha.zip](./captcha.zip)，其中有1000张验证码图片是手工标注的，其余将近2000多张验证码图片是通过模拟登陆进行验证，在验证码验证不通过时再进行人工标记而成的，总共比3000张少一点点，现在提供开源使用。

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
