import 'package:flutter/gestures.dart';
import 'package:flutter/material.dart';

import 'login_page.dart';

class SignUpPage extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        backgroundColor: Colors.white,
        centerTitle: true,
        title: Text(
          'Sign Up',
          style: TextStyle(fontWeight: FontWeight.bold, color: Colors.black),
        ),
        elevation: 0,
        iconTheme: IconThemeData(
          color: Colors.black, // Set the color of the arrow here
        ),
      ),
      body: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            SizedBox(height: 30),
            Text(
              'Full Name',
              style: TextStyle(
                fontWeight: FontWeight.bold,
              ),
            ),
            SizedBox(height: 8),
            TextField(
              decoration: InputDecoration(
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(30),
                ),
                hintText: 'Enter your full name',
              ),
            ),
            SizedBox(height: 16),
            Text(
              'Email',
              style: TextStyle(
                fontWeight: FontWeight.bold,
              ),
            ),
            SizedBox(height: 8),
            TextField(
              decoration: InputDecoration(
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(30),
                ),
                hintText: 'Enter your email',
              ),
            ),
            SizedBox(height: 16),
            Text(
              'Password',
              style: TextStyle(
                fontWeight: FontWeight.bold,
              ),
            ),
            SizedBox(height: 8),
            TextField(
              decoration: InputDecoration(
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(30),
                ),
                hintText: 'Enter your password',
              ),
            ),
            SizedBox(height: 16),
            Container(
              width: MediaQuery.of(context).size.width * 0.8,
              height: 100, // 80% of screen width
              child: Padding(
                padding: EdgeInsets.symmetric(
                  vertical: 20,
                ), // Top and bottom padding of 20px
                child: ElevatedButton(
                  child: Text(
                    'Sign Up',
                    style: TextStyle(
                      fontSize: 18, // Change this value to adjust the font size
                    ),
                  ),
                  
                  style: ButtonStyle(
                    elevation: MaterialStateProperty.all<double>(
                        0),
                    backgroundColor: MaterialStateProperty.all<Color>(
                      const Color.fromARGB(255, 37, 101, 240),
                    ), // Replace XXXXXX with your color hex value
                    shape: MaterialStateProperty.all<RoundedRectangleBorder>(
                      RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(
                          30, // the radius of the button corners
                        ),
                      ),
                    ),
                  ),
                  onPressed: () {
                    // Handle sign up
                  },
                ),
              ),
            ),
            Container(
              alignment: Alignment.bottomCenter,
              child: RichText(
                textAlign: TextAlign.center,
                text: TextSpan(
                  children: [
                    TextSpan(
                      text: 'Already have an account? ',
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
                            MaterialPageRoute(
                              builder: (context) => LoginPage(),
                            ),
                          );
                        },
                    ),
                  ],
                ),
              ),
            ),
            SizedBox(height: 32),
            Text(
              'OR',
              textAlign: TextAlign.center,
              style: TextStyle(
                fontWeight: FontWeight.bold,
              ),
            ),
            SizedBox(height: 16),
            Container(
              width: MediaQuery.of(context).size.width * 0.8,
              height: 100, // 80% of screen width
              child: Padding(
                padding: EdgeInsets.symmetric(
                    vertical: 20), // Top and bottom padding of 20px
                child: ElevatedButton(
                  child: Text(
                    'Continue with Google',
                    style: TextStyle(
                        color: Color.fromARGB(255, 1, 1, 1),
                        fontSize: 15,
                        fontWeight: FontWeight
                            .w700 // Change this value to adjust the font size
                        ),
                  ),
                  style: ButtonStyle(
                    elevation: MaterialStateProperty.all<double>(
                        0), // Remove the box shadow
                    backgroundColor: MaterialStateProperty.all<Color>(
                        Color.fromARGB(255, 255, 255,
                            255)), // Replace XXXXXX with your color hex value
                    shape: MaterialStateProperty.all<RoundedRectangleBorder>(
                      RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(
                            30), // the radius of the button corners
                        side: BorderSide(
                            color: const Color.fromARGB(
                                255, 168, 168, 168)), // Add a black border
                      ),
                    ),
                  ),
                  onPressed: () {
                    // Handle sign up
                  },
                ),
              ),
            ),
            Container(
              width: MediaQuery.of(context).size.width * 0.8,
              height: 100, // 80% of screen width
              child: Padding(
                padding: EdgeInsets.symmetric(
                    vertical: 20), // Top and bottom padding of 20px
                child: ElevatedButton(
                  child: Text(
                    'Continue with Facebook',
                    style: TextStyle(
                        color: Color.fromARGB(255, 1, 1, 1),
                        fontSize: 15,
                        fontWeight: FontWeight
                            .w700 // Change this value to adjust the font size
                        ),
                  ),
                  style: ButtonStyle(
                    elevation: MaterialStateProperty.all<double>(
                        0), // Remove the box shadow
                    backgroundColor: MaterialStateProperty.all<Color>(
                        Color.fromARGB(255, 249, 249,
                            249)), // Replace XXXXXX with your color hex value
                    shape: MaterialStateProperty.all<RoundedRectangleBorder>(
                      RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(
                            30), // the radius of the button corners
                        side: BorderSide(
                            color: const Color.fromARGB(
                                255, 168, 168, 168)), // Add a black border
                      ),
                    ),
                  ),
                  onPressed: () {
                    // Handle sign up
                  },
                ),
              ),
            ),
            Container(
              alignment: Alignment.bottomCenter,
              child: RichText(
                textAlign: TextAlign.center,
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
                      text: 'Sign Up',
                      style: TextStyle(
                        color: Color(0xFF2563EB),
                        fontWeight: FontWeight.w600,
                      ),
                      recognizer: TapGestureRecognizer()
                        ..onTap = () {
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                                builder: (context) => LoginPage()),
                          );
                        },
                    ),
                  ],
                ),
              ),
            )
          ],
        ),
      ),
    );
  }
}
