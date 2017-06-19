import os
from oic.oic import Client as OIDCClient
from oic.oic.message import AuthorizationResponse, IdToken, Message
from oic.utils.authn.client import CLIENT_AUTHN_METHOD

try:
    from oic.oauth2 import rndstr
except ImportError:
    import string
    import random

    def rndstr(size=16):
        """
        Returns a string of random ascii characters or digits
        :param size: The length of the string
        :return: string
        """
        _basech = string.ascii_letters + string.digits
        return "".join([random.choice(_basech) for _ in range(size)])

__author__ = 'regu0004'

class Client(object):
    # TODO specify the correct path
    ROOT_PATH = "/opt/openid_course/"
    # TODO specify the correct URL
    ISSUER = "https://mitreid.org/"

    flow = 'implicit'
    scope = ['openid', 'profile', 'email']

    def _get_response_type_for_auth(self):
        if self.flow == 'code':
            return 'code'
        elif self.flow == 'implicit':
            return ["id_token", "token"]
        else:
            return None

    def _get_redirect_uris_for_auth(self):
        if self.flow == 'code':
            return self.client.redirect_uris[0]
        elif self.flow == 'implicit':
            return self.client.redirect_uris[1]
        else:
            return None

    def __init__(self, client_metadata):
        self.flow = 'code'
        self.client = OIDCClient(client_authn_method=CLIENT_AUTHN_METHOD)

        # TODO get the provider configuration information
        uid = "fake_user@mitreid.org"
        #self.ISSUER = self.client.discover(uid)
        provider_info = self.client.provider_config(self.ISSUER)

        # TODO register with the provider using the client_metadata
        reg_endpoint = provider_info["registration_endpoint"]
        self.client.redirect_uris = client_metadata['redirect_uris']
        self.client.response_types = client_metadata['response_types']
        registration_response = self.client.register(reg_endpoint)

        # TODO check registration response
        reg_resp = Message()
        reg_resp.from_dict(dictionary=registration_response)
        reg_resp.verify()

    def authenticate(self, session):
        # Use the session object to store state between requests

        # TODO make authentication request
        session["state"] = rndstr()
        session["nonce"] = rndstr()
        request_args = {
            "client_id": self.client.client_id,
            "response_type": self._get_response_type_for_auth(),
            "scope": self.scope,
            "nonce": session["nonce"],
            "redirect_uri": self._get_redirect_uris_for_auth(),
            "state": session["state"]
        }

        auth_req = self.client.construct_AuthorizationRequest(request_args=request_args)
        login_url = auth_req.request(self.client.authorization_endpoint)
        return login_url

    def code_flow_callback(self, auth_response, session):
        # TODO parse the authentication response
        aresp = self.client.parse_response(AuthorizationResponse, info=auth_response, sformat="urlencoded")
        assert aresp["state"] == session["state"]

        # TODO make token request
        access_code = aresp["code"]
        args = {
            "code": access_code,
            "redirect_uri": self._get_redirect_uris_for_auth(),
            "client_id": self.client.client_id,
            "client_secret": self.client.client_secret
        }

        resp = self.client.do_access_token_request(scope=self.scope, #aresp["scope"],
                                                   state=aresp["state"],
                                                   request_args=args,
                                                   authn_method="client_secret_post"
                                                  )

        # TODO validate the ID Token according to the OpenID Connect spec (sec 3.1.3.7.)
        id_token_claims = IdToken()
        id_token_claims.from_dict(dictionary=resp['id_token'])
        id_token_claims.verify()

        # TODO make userinfo request
        userinfo = self.client.do_user_info_request(state=aresp["state"])

        # TODO set the appropriate values
        access_token = resp['access_token']
        return success_page(access_code, access_token, id_token_claims, userinfo)

    def implicit_flow_callback(self, auth_response, session):
        # TODO parse the authentication response
        aresp = self.client.parse_response(AuthorizationResponse, info=auth_response, sformat="urlencoded")
        assert aresp["state"] == session["state"]

        # TODO validate the ID Token according to the OpenID Connect spec (sec 3.2.2.11.)
        id_token_claims = IdToken()
        id_token_claims.from_dict(dictionary=aresp['id_token'])
        id_token_claims.verify()

        # TODO make userinfo request
        userinfo = self.client.do_user_info_request(state=aresp["state"], scope=self.scope)

        # TODO set the appropriate values
        access_code = None
        access_token = aresp['access_token']

        return success_page(access_code, access_token, id_token_claims, userinfo)


def success_page(auth_code, access_token, id_token_claims, userinfo):
    html_page = read_from_file("success_page.html")
    return html_page.format(auth_code, access_token, id_token_claims, userinfo)


def read_from_file(path):
    full_path = os.path.join(Client.ROOT_PATH, path)
    with open(full_path, "r") as f:
        return f.read()
