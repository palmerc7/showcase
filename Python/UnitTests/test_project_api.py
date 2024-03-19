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
project_api_url_prefix = 'https://showcase-api.azurewebsites.net/api/v1/publicproject'
project_public_api_url_prefix = 'https://showcase-apis-devtest.azure-api.net/api/v1/project'


def print_failed_test_message(message_to_print):
    print(colorama.Fore.RED + message_to_print)


def get_an_auth_token_for_projects():
    print(colorama.Fore.WHITE + '\n  Running get_an_auth_token_for_projects')

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


def test_that_project_apis_can_get_an_auth_token(debug=False):
    print(colorama.Fore.WHITE + '\n  Running test_that_project_apis_can_get_an_auth_token')

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


def test_that_project_api_gets_all_projects(api_url_prefix, access_token, debug=False):
    print(colorama.Fore.WHITE + '\n  Running test_that_project_api_gets_all_projects')

    project_api_url = project_api_url_prefix + '/get'
    if debug:
        print(colorama.Fore.WHITE + f'    project_api_url: {project_api_url}')

    headers = {'access_token': f'{access_token}'}
    response = requests.get(project_api_url, headers=headers, verify=False, cert=None)
    if debug:
        print(colorama.Fore.WHITE + f'    response_status_code: {response.status_code}')
        print(colorama.Fore.WHITE + f'    raw_response: {response.text}')

    #assert response.status_code == 201, "    FAILED: Project API did not return status code of 200"
    if response.status_code != 200:
        print_failed_test_message('    FAILED: Project API did not return status code of 200')
        raise AssertionError('FAILED: Project API did not return status code of 200')

    if debug:
        print(colorama.Fore.WHITE + f'    raw_response: {response.text}')

    response_as_dict = json.loads(response.text)
    response_data = response_as_dict['Data']
    if debug:
        print(colorama.Fore.WHITE + f'    response_data: {response_data}')
    assert len(response_data) > 1, "    Project API should have returned all projects, more than one without any filters"

    print(colorama.Fore.GREEN + f'  TEST PASSED')


def test_that_project_api_can_filter_projects(api_url_prefix, access_token, debug=False):
    print(colorama.Fore.WHITE + '\n  Running test_that_project_api_can_filter_projects')

    project_api_url = api_url_prefix + '/get'
    if debug:
        print(colorama.Fore.WHITE + f'    project_api_url: {project_api_url}')

    headers = {'Content-Type': 'application/json', 'access_token': f'{access_token}'}
    filter_data_query = {"Filters": [{"Key": "projectName","Value": "New Project"}]}
    project_api_url = project_api_url_prefix + '/getwithfilter'
    response = requests.post(project_api_url, data=json.dumps(filter_data_query), headers=headers, verify=False)
    if debug:
        print(colorama.Fore.WHITE + f'    response_status_code: {response.status_code}')
        print(colorama.Fore.WHITE + f'    raw_response: {response.text}')

    # assert response.status_code == 200, "    FAILED: Project API did not return status code of 200"
    if response.status_code != 200:
        print_failed_test_message('    FAILED: Project API did not return status code of 200')
        raise AssertionError('FAILED: Project API did not return status code of 200')

    if debug:
        print(colorama.Fore.WHITE + f'    raw_response: {response.text}')

    response_as_dict = json.loads(response.text)
    response_data = response_as_dict['Data']
    if debug:
        print(colorama.Fore.WHITE + f'    response_data: {response_data}')
    assert len(response_data) == 1, "    Project API should have returned only one project with passed filter"

    print(colorama.Fore.GREEN + f'  TEST PASSED')


class PublicProjectApiUnitTestSuite(unittest.TestCase):

    # Define all unit tests here
    def test_case1_project_auth_token(self):
        debug = False
        test_that_project_apis_can_get_an_auth_token(debug)

    def test_case2_project_api_gets_all(self):
        debug = False
        access_token = get_an_auth_token_for_projects()
        test_that_project_api_gets_all_projects(project_api_url_prefix, access_token, debug)
        test_that_project_api_gets_all_projects(project_public_api_url_prefix, access_token, debug)

    def test_case3_project_api_can_filter_projects(self):
        debug = True
        access_token = get_an_auth_token_for_projects()
        test_that_project_api_can_filter_projects(project_api_url_prefix, access_token, debug)
        test_that_project_api_can_filter_projects(project_public_api_url_prefix, access_token, debug)


if __name__ == '__main__':

    # Initialize colorama for console text colors
    colorama.init()

    print(colorama.Fore.WHITE + '\nRunning main - UNIT TESTS: test_project_api.py')

    # Call all unit tests defined in the PublicProjectApiUnitTestSuite class
    unittest.main()

    print(colorama.Fore.WHITE + '\nEND: main - UNIT TESTS: test_project_api.py')

    # Re-init colorama back to defaults
    colorama.reinit()
