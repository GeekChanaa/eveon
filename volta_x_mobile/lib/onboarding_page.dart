import 'package:flutter/gestures.dart';
import 'package:flutter/material.dart';
import 'package:volta_x_mobile/signup_page.dart';

import 'login_page.dart';

void main() {
  runApp(MyApp());
}

class MyApp extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Flutter Onboarding Demo',
      theme: ThemeData(
        primarySwatch: Colors.blue,
      ),
      home: OnboardingPage(),
    );
  }
}

class OnboardingPage extends StatefulWidget {
  @override
  _OnboardingPageState createState() => _OnboardingPageState();
}

class _OnboardingPageState extends State<OnboardingPage> {
  final _controller = PageController();
  int _currentPage = 0;

  List<Widget> _pages = [
    Padding(
      padding: EdgeInsets.only(bottom: 250, right: 70, left: 70), // bottom padding of 200
      child: Column(
        mainAxisAlignment:
            MainAxisAlignment.end, // content will start from the end
        children: <Widget>[
          Text(
            'The Best Place To Rest Your Body',
            style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
            textAlign: TextAlign.center,
          ),
          SizedBox(height: 20),
          Text(
            'Browse and build your collection of the world’s most cutting-edge digital art',
            textAlign: TextAlign.center,
          ),
        ],
      ),
    ),
    Padding(
      padding: EdgeInsets.only(bottom: 250, right: 70, left: 70), // bottom padding of 200
      child: Column(
        mainAxisAlignment:
            MainAxisAlignment.end, // content will start from the end
        children: <Widget>[
          Text(
            'It’s All About Scarity',
            style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
          ),
          SizedBox(height: 20),
          Text(
            'Art is minted as an NFT and comes as limited editions, making them extremely valuable.',
            textAlign: TextAlign.center,
          ),
        ],
      ),
    ),
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Stack(
        children: <Widget>[
          PageView(
            controller: _controller,
            children: _pages,
            onPageChanged: (index) {
              setState(() {
                _currentPage = index;
              });
            },
          ),
          Align(
            alignment: Alignment.bottomCenter,
            child: Padding(
              padding: EdgeInsets.all(20),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.end,
                children: <Widget>[
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children:
                        List<Widget>.generate(_pages.length, _buildIndicator),
                  ),
                  SizedBox(height: 20),
                  Container(
                    width: MediaQuery.of(context).size.width * 0.8,
                    height: 100, // 80% of screen width
                    child: Padding(
                      padding: EdgeInsets.symmetric(
                          vertical: 20), // Top and bottom padding of 20px
                      child: ElevatedButton(
                        child: Text(
                          'Get Started',
                          style: TextStyle(
                              fontSize:
                                  18), // Change this value to adjust the font size
                        ),
                        style: ButtonStyle(
                          backgroundColor: MaterialStateProperty.all<
                              Color>(const Color
                                  .fromARGB(255, 37, 101,
                              240)), // Replace XXXXXX with your color hex value
                          shape:
                              MaterialStateProperty.all<RoundedRectangleBorder>(
                            RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(
                                  30), // the radius of the button corners
                            ),
                          ),
                        ),
                        onPressed: () {
                          Navigator.push(
                                context,
                                MaterialPageRoute(builder: (context) => SignUpPage()),
                                );
                        },
                      ),
                    ),
                  ),
                  RichText(
                    text: TextSpan(
                      children: [
                        TextSpan(
                          text: 'Have an account? ',
                          style: TextStyle(
                            color: Color.fromARGB(255, 0, 0, 0),
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        TextSpan(
                          text: 'Sign In',
                          style: TextStyle(
                            color: Color(0xFF2563EB),
                            fontWeight: FontWeight.w600,
                          ),
                          recognizer: TapGestureRecognizer()
                            ..onTap = () {
                              Navigator.push(
                                context,
                                MaterialPageRoute(builder: (context) => LoginPage()),
                                );
                            },
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildIndicator(int index) {
    return Container(
      margin: EdgeInsets.all(4),
      width: 12,
      height: 12,
      decoration: BoxDecoration(
        color: _currentPage == index ? Colors.blue : Colors.grey,
        shape: BoxShape.circle,
      ),
    );
  }
}
