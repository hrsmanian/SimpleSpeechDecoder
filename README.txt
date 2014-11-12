SimpleSpeechDecoder
===================

This is a simple static network speech decoder written in C# and is for understanding purpose only. There are quite a few speech recognition systems like Sphinx, Julius, ISIP Decoder etc. which are open source and have many features. However, it is not always easy to use these systems to understand basics of Speech Recognition since they are rich in features and hence have quite a lot of code. Hence the aim is to build a simple system that anyone new to speech recognition can follow.

Models: Monophone models are provided for TIDIGITS database. The models have been trained using ISIP speech recognizer using a subset of TIDIGITS data. I am very thankful to my alumni for making it easier to train the models. The packaged Perl Script enables anyone to train Word, monophone, word-internal and cross-word models. 

Test Data: Test data is also obtained from the packaged Tidigits tutorial provided by ISIP. A sample of 10 utterances is provided for testing purposes.

How to run:
===========

Once you have cloned the Git repository, you can run the provided executable as

SimpleSpeechDecoder.exe data\lexicon.text data\models.text data\states.text data\transitions.text data\networ
k2.txt data\ar_oa.mfc

You can also build a C# project using the .cs files provided and compile them. And then run the executable as mentioned above.

Acknowledgements:
=================

Special thanks for members of ISIP for the prototype system and the packaged TIDIGITS tutorial. Saved me a lot of time. 

Feedback:
=========

Kindly direct your questions, suggestions and comments to my email: hrsmanian@gmail.com

