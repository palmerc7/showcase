import colorama
import unittest

if __name__ == '__main__':

    # Initialize colorama for console text colors
    colorama.init()

    print(colorama.Fore.WHITE + '\nRunning main - UNIT TEST SUITE: test_suite.py')

    # Create a test suite
    suite = unittest.TestLoader().discover(start_dir='.', pattern='test_*.py')
    
    # Run the test suite
    result = unittest.TextTestRunner().run(suite)

    # Exit with 1 if there are init test failures
    if not result.wasSuccessful():
        print(colorama.Fore.GREEN + f'  TEST SUITE FAILED, exiting with code 1')
        exit(1)
    else:
        print(colorama.Fore.GREEN + f'  TEST SUITE PASSED, exiting with code 0')
        exit(0)
