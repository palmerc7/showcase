import colorama
import json
import requests
import unittest
import urllib3

# Disable warnings for insecure requests in unit tests
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

# Static variables for local testing
var_client_id = ''
var_grant_type = 'client_credentials'
var_client_secret = ''

# Static URLs for API unit testing
auth_token_public_api_url = 'https://showcase-apis-devtest.azure-api.net/api/v1/auth/token'
vendor_api_url_prefix = 'https://showcasedev-api.azurewebsites.net/api/v1/publicvendor'
vendor_public_api_url_prefix = 'https://showcase-apis-devtest.azure-api.net/api/v1/vendor'


def print_failed_test_message(message_to_print):
    print(colorama.Fore.RED + message_to_print)


def get_an_auth_token_for_vendors():
    print(colorama.Fore.WHITE + '\n  Running get_an_auth_token_for_vendors')

    auth_token_api_url = auth_token_public_api_url
    headers = {'Content-Type': 'application/x-www-form-urlencoded'}
    auth_parameters = {
        'client_id': var_client_id,
        'grant_type': var_grant_type,
        'client_secret': var_client_secret
    }

    response = requests.post(auth_token_api_url, data=auth_parameters, headers=headers, verify=False)
    response_as_dict = json.loads(response.text)
    response_data = response_as_dict['access_token']
    return response_data


def test_that_vendor_apis_can_get_an_auth_token(debug=False):
    print(colorama.Fore.WHITE + '\n  Running test_that_vendor_apis_can_get_an_auth_token')

    auth_token_api_url = auth_token_public_api_url
    if debug:
        print(colorama.Fore.WHITE + f'    auth_token_api_url: {auth_token_api_url}')

    headers = {'Content-Type': 'application/x-www-form-urlencoded'}
    auth_parameters = {
        'client_id': var_client_id,
        'grant_type': var_grant_type,
        'client_secret': var_client_secret
    }

    response = requests.post(auth_token_api_url, data=auth_parameters, headers=headers, verify=False)
    if debug:
        print(colorama.Fore.WHITE + f'    response_status_code: {response.status_code}')
        print(colorama.Fore.WHITE + f'    raw_response: {response.text}')

    assert response.status_code == 200, "    FAILED: Auth Token API did not return status code of 200"

    if debug:
        print(colorama.Fore.WHITE + f'    raw_response: {response.text}')

    response_as_dict = json.loads(response.text)
    response_data = response_as_dict['access_token']
    if debug:
        print(colorama.Fore.WHITE + f'    response_data: {response_data}')
    assert response_data is not None, "    Auth Token API should have returned non-empty response data"

    print(colorama.Fore.GREEN + f'  TEST PASSED')


def test_that_vendor_api_gets_all_vendors(api_url_prefix, access_token, debug=False):
    print(colorama.Fore.WHITE + '\n  Running test_that_vendor_api_gets_all_vendors')

    vendor_api_url = api_url_prefix + '/get'
    if debug:
        print(colorama.Fore.WHITE + f'    vendor_api_url: {vendor_api_url}')

    headers = {'access_token': f'{access_token}'}
    response = requests.get(vendor_api_url, headers=headers, verify=False, cert=None)
    if debug:
        print(colorama.Fore.WHITE + f'    response_status_code: {response.status_code}')
        print(colorama.Fore.WHITE + f'    raw_response: {response.text}')

    #assert response.status_code == 201, "    FAILED: Vendor API did not return status code of 200"
    if response.status_code != 200:
        print_failed_test_message('    FAILED: Vendor API did not return status code of 200')
        raise AssertionError('FAILED: Vendor API did not return status code of 200')

    if debug:
        print(colorama.Fore.WHITE + f'    raw_response: {response.text}')

    response_as_dict = json.loads(response.text)
    response_data = response_as_dict['Data']
    if debug:
        print(colorama.Fore.WHITE + f'    response_data: {response_data}')
    assert len(response_data) > 1, "    Vendor API should have returned all vendors, more than one without any filters"

    print(colorama.Fore.GREEN + f'  TEST PASSED')


def test_that_vendor_api_can_filter_vendors(api_url_prefix, access_token, debug=False):
    print(colorama.Fore.WHITE + '\n  Running test_that_vendor_api_can_filter_vendors')

    vendor_api_url = api_url_prefix + '/get'
    if debug:
        print(colorama.Fore.WHITE + f'    vendor_api_url: {vendor_api_url}')

    headers = {'Content-Type': 'application/json', 'access_token': f'{access_token}'}
    filter_data_query = {"Filters": [{"Key": "vendorReference","Value": "ABC"}]}
    vendor_api_url = vendor_api_url_prefix + '/getwithfilter'
    response = requests.post(vendor_api_url, data=json.dumps(filter_data_query), headers=headers, verify=False)
    if debug:
        print(colorama.Fore.WHITE + f'    response_status_code: {response.status_code}')
        print(colorama.Fore.WHITE + f'    raw_response: {response.text}')

    # assert response.status_code == 200, "    FAILED: Vendor API did not return status code of 200"
    if response.status_code != 200:
        print_failed_test_message('    FAILED: Vendor API did not return status code of 200')
        raise AssertionError('FAILED: Vendor API did not return status code of 200')

    if debug:
        print(colorama.Fore.WHITE + f'    raw_response: {response.text}')

    response_as_dict = json.loads(response.text)
    response_data = response_as_dict['Data']
    if debug:
        print(colorama.Fore.WHITE + f'    response_data: {response_data}')
    assert len(response_data) == 1, "    Vendor API should have returned only one vendor with passed filter"

    print(colorama.Fore.GREEN + f'  TEST PASSED')


class PublicVendorApiUnitTestSuite(unittest.TestCase):

    # Define all unit tests here
    def test_case1_vendor_auth_token(self):
        debug = False
        test_that_vendor_apis_can_get_an_auth_token(debug)

    def test_case2_vendor_api_gets_all(self):
        debug = False
        access_token = get_an_auth_token_for_vendors()
        test_that_vendor_api_gets_all_vendors(vendor_api_url_prefix, access_token, debug)
        test_that_vendor_api_gets_all_vendors(vendor_public_api_url_prefix, access_token, debug)

    def test_case3_vendor_api_can_filter_vendors(self):
        debug = True
        access_token = get_an_auth_token_for_vendors()
        test_that_vendor_api_can_filter_vendors(vendor_api_url_prefix, access_token, debug)
        test_that_vendor_api_can_filter_vendors(vendor_public_api_url_prefix, access_token, debug)


if __name__ == '__main__':

    # Initialize colorama for console text colors
    colorama.init()

    print(colorama.Fore.WHITE + '\nRunning main - UNIT TESTS: test_vendor_api.py')

    # Call all unit tests defined in the PublicVendorApiUnitTestSuite class
    unittest.main()

    print(colorama.Fore.WHITE + '\nEND: main - UNIT TESTS: test_vendor_api.py')

    # Re-init colorama back to defaults
    colorama.reinit()
